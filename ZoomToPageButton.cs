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
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MapSeriesTools
{
    internal class ZoomToPageButton : Button
    {
        protected override async void OnClick()
        {
            // Pull module settings
            Dictionary<string, string> settings = MapSeriesTools.Current.Settings;

            // Check for selected layout
            LayoutProjectItem lytItem;
            if (settings.ContainsKey("SelectedMapSeries"))
            {
                await QueuedTask.Run(() =>
                {
                    // Get layout
                    lytItem = Project.Current
                    .GetItems<LayoutProjectItem>()
                    .FirstOrDefault(item => item.Name.Contains(settings["SelectedMapSeries"]));
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
                });
            }

        }
    }
}
