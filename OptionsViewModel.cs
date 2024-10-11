using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.KnowledgeGraph;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MapSeriesTools
{
    internal class OptionsViewModel : Page
    {
        /// <summary>
        /// Setting variables accessed by the options Page
        /// </summary>
        private bool _zoomToPageFlag;
        public bool ZoomToPageFlag
        {
            get { return _zoomToPageFlag; }
            set
            {
                if (SetProperty(ref _zoomToPageFlag, value, () => ZoomToPageFlag))
                    //You must set "IsModified = true" to have your CommitAsync called
                    base.IsModified = true;
            }
        }

        private string _selectedMapSeries;
        public string SelectedMapSeries
        {
            get { return _selectedMapSeries; }
            set
            {
                if (SetProperty(ref _selectedMapSeries, value, () => SelectedMapSeries))
                    //You must set "IsModified = true" to have your CommitAsync called
                    base.IsModified = true;
            }
        }

        private List<string> _mapSeriesList;
        public List<string> MapSeriesList
        {
            get { return _mapSeriesList; }
            set
            {
                if (SetProperty(ref _mapSeriesList, value, () => MapSeriesList))
                    //You must set "IsModified = true" to have your CommitAsync called
                    base.IsModified = true;
            }
        }

        /// <summary>
        /// Called when the page loads because it has become visible.
        /// </summary>
        /// <returns>A task that represents the work queued to execute in the ThreadPool.</returns>
        private bool _orig_zoomToPageFlag = true;
        private string _orig_selectedMapSeries = "";
        protected override Task InitializeAsync()
        {
            // Fetch list of layouts for combo box drop down
            //List<string> _layoutsWithMapSeries = Project.Current.GetItems<LayoutProjectItem>().ToList()
            //    .Where(item => item.GetLayout().MapSeries != null).ToList()
            // /\ Needs to be called on same thread; study multithreading & async
            //    .Select(item => item.Name)
            //    .ToList();

            //List<string> _layoutsWithMapSeries = new List<string>();

            //List<string> _layoutsWithMapSeries = new List<string>();

            List<string> _layoutsWithMapSeries = new List<string>();

            _layoutsWithMapSeries = Project.Current
            .GetItems<LayoutProjectItem>()
            .Select(item => item.Name)
            .ToList();

            // Store in list to filter combo box drop down
            MapSeriesList = _layoutsWithMapSeries;

            // Get project-specific settings
            Dictionary<string, string> settings = MapSeriesTools.Current.Settings;

            // assign to the values biniding to the controls
            if (settings.ContainsKey("ZoomToPageFlag"))
                ZoomToPageFlag = System.Convert.ToBoolean(settings["ZoomToPageFlag"]);
            else
                ZoomToPageFlag = _orig_zoomToPageFlag;

            if (settings.ContainsKey("SelectedMapSeries"))
                SelectedMapSeries = settings["SelectedMapSeries"];
            else
                SelectedMapSeries = _layoutsWithMapSeries[0];

            // keep track of the original values (used for comparison when saving)
            _orig_zoomToPageFlag = ZoomToPageFlag;
            _orig_selectedMapSeries = SelectedMapSeries;

            return Task.FromResult(true);
        }

        // Determines if the current settings are different from the original.
        private bool IsDirty()
        {
            if (_orig_zoomToPageFlag != ZoomToPageFlag)
                return true;
            if (_orig_selectedMapSeries != SelectedMapSeries)
                return true;
            return false;
        }
        /// <summary>
        /// Invoked when the OK or apply button on the property sheet has been clicked.
        /// </summary>
        /// <returns>A task that represents the work queued to execute in the ThreadPool.</returns>
        /// <remarks>This function is only called if the page has set its IsModified flag to true.</remarks>


        protected override Task CommitAsync()
        {
            if (IsDirty())
            {
                // store the new settings in the dictionary ... save happens in OnProjectSave
                Dictionary<string, string> settings = MapSeriesTools.Current.Settings;

                if (settings.ContainsKey("ZoomToPageFlag"))
                    settings["ZoomToPageFlag"] = ZoomToPageFlag.ToString();
                else
                    settings.Add("ZoomToPageFlag", ZoomToPageFlag.ToString());

                if (settings.ContainsKey("SelectedMapSeries"))
                    settings["SelectedMapSeries"] = SelectedMapSeries;
                else
                    settings.Add("SelectedMapSeries", SelectedMapSeries);

                // set the project dirty
                Project.Current.SetDirty(true);
            }
            return Task.FromResult(0);
        }

        /// <summary>
        /// Called when the page is destroyed.
        /// </summary>
        protected override void Uninitialize()
        {
        }
    }

    /// <summary>
    /// Button implementation to show the property sheet.
    /// </summary>
    internal class Options_ShowButton : Button
    {
        protected override void OnClick()
        {
            if (!PropertySheet.IsVisible)
                PropertySheet.ShowDialog("MapSeriesTools_OptionsSheet", "MapSeriesTools_OptionsPage");

        }
    }
}
