using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.CIM;
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
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace MapSeriesTools
{
    internal class NextPageButton : Button
    {
        protected override async void OnClick()
        {
            // Pull module settings
            Dictionary<string, string> settings = MapSeriesTools.Current.Settings;

            if (settings.ContainsKey("SelectedMapSeries") && settings.ContainsKey("ZoomToPageFlag"))
            {
                await QueuedTask.Run(async () =>
                {
                    // Get map series
                    MapSeries MS = Project.Current
                        .GetItems<LayoutProjectItem>()
                        .FirstOrDefault(item => item.Name.Contains(settings["SelectedMapSeries"]))
                        .GetLayout()
                        .MapSeries;

                    if (MS != null)
                    {
                        // Set current page to next page 
                        MS.SetCurrentPageNumber(MS.NextPageNumber);

                        if (bool.Parse(settings["ZoomToPageFlag"]))
                            MapSeriesTools.Current.zoom_to_map_series_page(MS);
                    }
                });
            }
        }
    }
}
