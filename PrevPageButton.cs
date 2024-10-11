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
using System.Threading.Tasks;

namespace MapSeriesTools
{
    internal class PrevPageButton : Button
    {
        protected override async void OnClick()
        {
            // Pull module settings
            Dictionary<string, string> settings = MapSeriesTools.Current.Settings;

            // Check for selected layout
            LayoutProjectItem lytItem;
            if (settings.ContainsKey("SelectedMapSeries") && settings.ContainsKey("ZoomToPageFlag"))
            {

                lytItem = Project.Current.GetItems<LayoutProjectItem>()
                         .FirstOrDefault(item => item.Name.Contains(settings["SelectedMapSeries"]));

                await QueuedTask.Run(() =>
                {
                    // Get layout
                    Layout map_series_layout = lytItem.GetLayout();
                    MapSeries MS = map_series_layout.MapSeries;

                    if (MS != null)
                    {
                        // Set current page to previous page 
                        MS.SetCurrentPageNumber(MS.PreviousPageNumber);


                        if (bool.Parse(settings["ZoomToPageFlag"]))
                            MapSeriesTools.zoom_to_map_series_page();
                    }

                });
            }
        
        }
    }
}
