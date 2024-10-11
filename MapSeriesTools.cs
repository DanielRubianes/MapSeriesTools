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
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;

namespace MapSeriesTools
{
    internal class MapSeriesTools : Module
    {
        private static MapSeriesTools _this = null;

        /// <summary>
        /// Retrieve the singleton instance to this module here
        /// </summary>
        public static MapSeriesTools Current => _this ??= (MapSeriesTools)FrameworkApplication.FindModule("MapSeriesTools_Module");

        private Dictionary<string, string> _moduleSettings = new Dictionary<string, string>();
        internal Dictionary<string, string> Settings
        {
            get { return _moduleSettings; }
            set { _moduleSettings = value; }
        }

        // Call on MCT Thread
        internal static void zoom_to_map_series_page()
        {
            // Get layout
            LayoutProjectItem lytItem = Project.Current
            .GetItems<LayoutProjectItem>()
            .FirstOrDefault(item => item.Name.Contains(Current.Settings["SelectedMapSeries"]));
            Layout map_series_layout = lytItem.GetLayout();
            MapSeries MS = map_series_layout.MapSeries;

            if (MS != null)
            {
                // Get map frame and view from map series object
                MapFrame map_frame = MS.MapFrame;
                MapView active_map = MapView.Active;

                Camera MS_Camera = map_frame.Camera;
                // Zoom out a bit
                MS_Camera.Scale = MS_Camera.Scale * 1.5;
                active_map.ZoomTo(MS_Camera, TimeSpan.Zero);
            }
        }

        #region Overrides

        private bool hasSettings = false;
        /// <summary>
        /// Framework will invoke this method on our Module when the project is opened or at any time when Pro reads the project settings.
        /// </summary>
        protected override Task OnReadSettingsAsync(ModuleSettingsReader settings)
        {
            // set the flag
            hasSettings = true;
            // clear existing setting values
            _moduleSettings.Clear();

            if (settings == null) return Task.FromResult(0);

            // Settings defined in the Property sheet’s viewmodel.	
            string[] keys = new string[] { "ZoomToPageFlag", "SelectedMapSeries" };
            foreach (string key in keys)
            {
                object value = settings.Get(key);
                if (value != null)
                {
                    if (_moduleSettings.ContainsKey(key))
                        _moduleSettings[key] = value.ToString();
                    else
                        _moduleSettings.Add(key, value.ToString());
                }
            }
            return Task.FromResult(0);
        }

        /// <summary>
        /// Framework calls this method on our Module any time project settings are to be saved.
        /// </summary>
        protected override Task OnWriteSettingsAsync(ModuleSettingsWriter settings)
        {
            foreach (string key in _moduleSettings.Keys)
            {
                settings.Add(key, _moduleSettings[key]);
            }
            return Task.FromResult(0);
        }

        /// <summary>
        /// Called by Framework when ArcGIS Pro is closing
        /// </summary>
        /// <returns>False to prevent Pro from closing, otherwise True</returns>
        protected override bool CanUnload()
        {
            //TODO - add your business logic
            //return false to ~cancel~ Application close
            return true;
        }

        #endregion Overrides

    }
}
