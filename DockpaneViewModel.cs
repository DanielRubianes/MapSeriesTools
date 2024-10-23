using ActiproSoftware.Windows.Extensions;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Events;
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;

namespace MapSeriesTools
{
    internal class DockpaneViewModel : DockPane
    {
        /// TODO: add event handlers for layout removed, added, map series changes

        private const string _dockPaneID = "MapSeriesTools_Dockpane";
        private object _lock = new();
        private ObservableCollection<string> _layoutList = new();
        private ObservableCollection<int> _pageNumbers = new();

        // Init
        protected DockpaneViewModel() {
            BindingOperations.EnableCollectionSynchronization(_layoutList, _lock);
            BindingOperations.EnableCollectionSynchronization(_pageNumbers, _lock);
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

        public ObservableCollection<int> PageNumbers
        {
            get { return _pageNumbers; }
        }

        private string _selectedLayout = "";
        public string SelectedLayout
        {
            get { return _selectedLayout; }
            set
            {
                SetProperty(ref _selectedLayout, value, () => SelectedLayout);
                OnLayoutSelected(value);
            }
        }

        private int _currentPage;
        public int CurrentPage
        {
            get { return _currentPage; }
            set
            {
                SetProperty(ref _currentPage, value, () => CurrentPage);
            }
        }
        private int _lastPage;
        public int LastPage
        {
            get { return _lastPage; }
            set
            {
                SetProperty(ref _lastPage, value, () => LastPage);
            }
        }

        #region Event Handlers

        // TODO: Detect if selected layout was actually changed
        private void OnLayoutSelected(string layoutName)
        {
            MapSeries MS = Project.Current
                        .GetItems<LayoutProjectItem>()
                        .FirstOrDefault( item => item.Name.Contains(layoutName) )
                        .GetLayout()
                        .MapSeries;
            if (MS == null)
                return;
            List<int> ms_page_numbers = Enumerable.Range(int.Parse(MS.CurrentPageNumber), int.Parse(MS.LastPageNumber)).ToList();
            PageNumbers.Clear();
            PageNumbers.AddRange(ms_page_numbers);

            CurrentPage = int.Parse(MS.CurrentPageNumber);
            LastPage = int.Parse(MS.LastPageNumber);
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
                    .Where(item => item.GetLayout()?.MapSeries != null)
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
