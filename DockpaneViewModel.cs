using ActiproSoftware.Windows.Extensions;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.CIM;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Core.Utilities;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Mapping.Symbology;
using ArcGIS.Desktop.KnowledgeGraph;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace MapSeriesTools
{
    internal class DockpaneViewModel : DockPane
    {
        /// TODO: add event handlers for layout removed, added, map series changes

        private const string _dockPaneID = "MapSeriesTools_Dockpane";
        private object _lock = new();
        private ObservableCollection<string> _layoutList = new();
        private ObservableCollection<MS_Page> _pageList = new();

        // Init
        protected DockpaneViewModel() {
            BindingOperations.EnableCollectionSynchronization(_layoutList, _lock);
            BindingOperations.EnableCollectionSynchronization(_pageList, _lock);
            ProjectItemsChangedEvent.Subscribe(OnProjectItemsChanged);
            ProjectOpenedEvent.Subscribe(OnProjectOpened);

            // If ViewModel initialized after project is opened
            if (Project.Current != null)
            {
                ProjectEventArgs args = new(Project.Current);
                OnProjectOpened(args);
            }
        }

        public ObservableCollection<string> LayoutList
        {
            get { return _layoutList; }
        }

        private string _selectedLayout = "";
        public string SelectedLayout
        {
            get { return _selectedLayout; }
            set
            {
                OnLayoutSelected(value);
                SetProperty(ref _selectedLayout, value, () => SelectedLayout);
            }
        }

        private Boolean _zoomToPageFlag = true;
        public Boolean ZoomToPageFlag
        {
            get { return _zoomToPageFlag; }
            set
            {
                SetProperty(ref _zoomToPageFlag, value, () => ZoomToPageFlag);
            }
        }

        public class MS_Page
        // PageNumber is an integer; matching map series implementation
        {
            public string PageName { get; set; }
            public string PageNumber { get; set; }

            public MS_Page(string page_name, string page_number)
            {
                PageName = page_name;
                PageNumber = page_number;
            }
        }

        public ObservableCollection<MS_Page> PageList
        {
            get { return _pageList; }
        }

        private MS_Page _currentPage;
        public MS_Page CurrentPage
        {
            get { return _currentPage; }
            set
            {
                GoToPage(value);
                SetProperty(ref _currentPage, value, () => CurrentPage);
            }
        }

        #region Button Commands

        public ICommand CmdZoomToPage
        {
            get
            {
                return new RelayCommand(() =>
                {
                    ZoomToPage(SelectedLayout);
                }, true);
            }
        }

        //public ICommand CmdZoomToPage
        //{
        //    get
        //    {
        //        if (update == null)
        //        {
        //            update = new RelayCommand(
        //                async () =>
        //                {
        //                    updateInProgress = true;
        //                    CmdZoomToPage.RaiseCanExecuteChanged();

        //                    await Task.Run(() => StartUpdate());

        //                    updateInProgress = false;
        //                    CmdZoomToPage.RaiseCanExecuteChanged();
        //                },
        //                () => !updateInProgress);
        //        }
        //        return update;
        //    }
        //}

        public ICommand CmdNextPage
        {
            get
            {
                return new RelayCommand(() =>
                {
                    int current_page_idx = PageList.IndexOf(CurrentPage);
                    if (current_page_idx != -1 && current_page_idx < PageList.Count - 1)
                    {
                        CurrentPage = PageList[current_page_idx + 1];
                    }
                }, true);
            }
        }

        public ICommand CmdPrevPage
        {
            get
            {
                return new RelayCommand(() =>
                {
                    int current_page_idx = PageList.IndexOf(CurrentPage);
                    if ( current_page_idx != -1 && current_page_idx > 0 )
                        {
                            CurrentPage = PageList[current_page_idx - 1];
                        }
                    }, true);
                }
        }

        #endregion

        #region Event Handlers

        // Poulate page list and selected page to reflect current spatial map sereis page name
        private void OnLayoutSelected(string layoutName)
        {
            QueuedTask.Run(() =>
            {
                // This fails because of multithreading
                //if (layoutName == SelectedLayout)
                //    return;

                MapSeries MS = Project.Current.GetItems<LayoutProjectItem>()
                    .FirstOrDefault(item => item.Name.Contains(layoutName))
                    .GetLayout().MapSeries;
                SpatialMapSeries SMS = MS as SpatialMapSeries;

                PageList.Clear();
                CurrentPage = null;

                if (SMS == null)
                    return;

                CurrentPage = new MS_Page(
                    MS.CurrentPageName,
                    MS.CurrentPageNumber
                );

                String sort_field = SMS.SortField;
                ArcGIS.Core.Data.QueryFilter page_order = new()
                {
                    PostfixClause = $"ORDER BY {sort_field}"
                };

                // Default list of pages if no page number specified
                var page_counter = Enumerable.Range(int.Parse(MS.FirstPageNumber), MS.PageCount).Select(page => page.ToString())
                                       .ToList().GetEnumerator();

                String number_field = SMS.PageNumberField;
                String name_field = SMS.PageNameField;
                using (RowCursor frame_cursor = SMS.IndexLayer.Search(page_order))
                {
                    while (frame_cursor.MoveNext())
                    {
                        Row row = frame_cursor.Current;
                        String page_number;
                        if (number_field == "" || number_field == null)
                        {
                            //ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"null", "Number Field");
                            page_counter.MoveNext();
                            page_number = page_counter.Current;
                        }
                        else
                        {
                            //ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"not null", "Number Field");
                            page_number = row[number_field].ToString();
                        }
                        
                        PageList.Add(new MS_Page(
                            row[name_field].ToString(),
                            page_number
                        ));
                    }
                }
            });
        }

        // Select spatial map series page based on page name field
        private void GoToPage(MS_Page page)
        {
            if (page == null)
                return;
            QueuedTask.Run(() =>
            {
                MapSeries MS = Project.Current.GetItems<LayoutProjectItem>()
                    .FirstOrDefault(item => item.Name.Contains(SelectedLayout))
                    .GetLayout().MapSeries;
                if (MS == null)
                    return;
                if (MS.CurrentPageNumber == page.PageNumber)
                    return;
                MS.SetCurrentPageNumber(page.PageNumber);

                if (ZoomToPageFlag)
                    ZoomToPage(SelectedLayout);
            });
        }

        private async void ZoomToPage(string layout_name)
        {
            await QueuedTask.Run(() =>
            {
                //MessageBox.Show("TEST");
                // Get map series
                MapSeries MS = Project.Current
                .GetItems<LayoutProjectItem>()
                .FirstOrDefault(item => item.Name.Contains(SelectedLayout))
                .GetLayout()
                .MapSeries;

                if (MS != null)
                {
                    // Get map frame and view from map series object
                    Camera MS_Camera = MS.MapFrame.Camera;
                    MS_Camera.Scale = MS_Camera.Scale * 1.5; // Zoom out a bit
                    MapView.Active.ZoomTo(MS_Camera, TimeSpan.Zero);
                }
            });
        }

        private void OnProjectItemsChanged(ProjectItemsChangedEventArgs args)
        {
            if (!(args.ProjectItem is LayoutProjectItem layoutProjectItem)) 
                return;

            if (args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                var layoutName = layoutProjectItem.Name;
                FrameworkApplication.Current.Dispatcher.Invoke((Action)delegate
                {
                    LayoutList.Add(layoutName);
                });
                System.Diagnostics.Debug.WriteLine($"Layout Added: {layoutName}");
            }
            else if (args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                var layoutName = layoutProjectItem.Name;
                FrameworkApplication.Current.Dispatcher.Invoke((Action)delegate
                {
                    LayoutList.Remove(layoutName);
                });
                System.Diagnostics.Debug.WriteLine($"Layout Added: {layoutName}");
            }
        }

        private void OnProjectOpened(ProjectEventArgs args)
        {
            List<string> _projectLayouts = new();
            QueuedTask.Run(() =>
            {
                _projectLayouts = args.Project
                    .GetItems<LayoutProjectItem>()
                    //.Where(item => item.GetLayout()?.MapSeries != null)
                    .Select(item => item.Name)
                    .ToList();

                LayoutList.Clear();
                if (_projectLayouts.Count > 0)
                    LayoutList.AddRange(_projectLayouts);
            });
        }

        #endregion

        /// <summary>
        /// Show the DockPane.
        /// </summary>
        internal static void Show()
        {
            DockPane pane = FrameworkApplication.DockPaneManager.Find(_dockPaneID);
            if (pane == null)
                return;

            pane.Activate();
        }

        /// <summary>
        /// Text shown near the top of the DockPane.
        /// </summary>
        private string _heading = "Map Series";
        public string Heading
        {
            get => _heading;
            set => SetProperty(ref _heading, value);
        }
    }

    /// <summary>
    /// Button implementation to show the DockPane.
    /// </summary>
    internal class Dockpane_ShowButton : Button
    {
        protected override void OnClick()
        {
            DockpaneViewModel.Show();
        }
    }
}
