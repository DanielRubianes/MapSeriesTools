/*

   Copyright 2023 Esri

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       https://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

   See the License for the specific language governing permissions and
   limitations under the License.

*/
using ActiproSoftware.Windows.Extensions;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Mapping;
using ArcGIS.Desktop.Layouts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;

namespace MapSeriesTools
{
    public class PageDialogViewModel : ViewModelBase
    {
        PageDialog _view = null;

        private object _lock = new();
        private ObservableCollection<MS_Page> _pageList = new();
        private Boolean _pagesNeeded = true;

        public PageDialogViewModel(PageDialog view)
        {
            BindingOperations.EnableCollectionSynchronization(_pageList, _lock);
            _view = view;
        }

        public class MS_Page
        // PageNumber is a string; matching API MapSeries implementation
        {
            public string PageName { get; set; }
            public string PageNumber { get; set; }

            public MS_Page(string page_name, string page_number)
            {
                PageName = page_name;
                PageNumber = page_number; 
            }
        }

        private List<MS_Page> get_pages()
        {
            // Pull module settings
            Dictionary<string, string> settings = MapSeriesTools.Current.Settings;

            if ( !settings.ContainsKey("SelectedMapSeries") || !settings.ContainsKey("ZoomToPageFlag") )
            {
                return new List<MS_Page>();
            }
            var page_list = new List<MS_Page>();
            Task page_find = QueuedTask.Run(() =>
            {
                MapSeries MS = Project.Current.GetItems<LayoutProjectItem>()
                    .FirstOrDefault(item => item.Name.Contains(settings["SelectedMapSeries"]))
                    .GetLayout().MapSeries;
                SpatialMapSeries SMS = MS as SpatialMapSeries;

                if (SMS == null)
                    return;

                String sort_field = SMS.SortField;
                ArcGIS.Core.Data.QueryFilter page_order = new()
                {
                    PostfixClause = $"ORDER BY {sort_field}"
                };

                // Default list of pages if no page number specified
                var page_counter = Enumerable.Range(int.Parse(MS.FirstPageNumber), MS.PageCount).Select(page => page.ToString()).ToList().GetEnumerator();

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

                        page_list.Add(new MS_Page(
                            row[name_field].ToString(),
                            page_number
                        ));
                    }
                }
            });
            page_find.Wait();
            return page_list;
        }

        public ObservableCollection<MS_Page> PageList
        {
            get
            {
                if (_pagesNeeded)
                {
                    _pageList.Clear();
                    _pageList.AddRange(get_pages());
                    _pagesNeeded = false;
                }
                return _pageList;
            }
        }

        private MS_Page _selectedPage = null;
        public MS_Page SelectedPage
        {
            get { return _selectedPage; }
            set { SetProperty(ref _selectedPage, value, () => SelectedPage); }
        }

        // TODO: Convert to ICommand
        public ICommand CmdOk => new RelayCommand(async () =>
        {
            // Pull module settings
            Dictionary<string, string> settings = MapSeriesTools.Current.Settings;

            if (settings.ContainsKey("SelectedMapSeries") && settings.ContainsKey("ZoomToPageFlag"))
            {
                await QueuedTask.Run(() =>
                {
                    // Get map series
                    MapSeries MS = Project.Current
                        .GetItems<LayoutProjectItem>()
                        .FirstOrDefault(item => item.Name.Contains(settings["SelectedMapSeries"]))
                        .GetLayout()
                        .MapSeries;

                    if (MS != null && SelectedPage != null)
                    {
                        // Set current page to next page 
                        MS.SetCurrentPageNumber(SelectedPage.PageNumber);

                        if (bool.Parse(settings["ZoomToPageFlag"]))
                            MapSeriesTools.Current.zoom_to_map_series_page(MS);
                    }
                });
            }
            _view.DialogResult = true;
            _view.Close();
        });

        public ICommand CmdCancel => new RelayCommand(() =>
        {
            _view.DialogResult = false;
            _view.Close();
        });
    }

    /// <summary>
    /// Button implementation to show the Dialog.
    /// </summary>
    internal class GoToPageButton : Button
    {
        private PageDialog _prowindowMvvm = null;
        protected override void OnClick()
        {
            //already open?
            if (_prowindowMvvm != null)
                return;
            _prowindowMvvm = new PageDialog();

            _prowindowMvvm.Owner = FrameworkApplication.Current.MainWindow;
            _prowindowMvvm.Closed += (o, e) => { _prowindowMvvm = null; };
            //_prowindowMvvm.Show();
            //uncomment for modal
            _prowindowMvvm.ShowDialog();
        }
    }
}