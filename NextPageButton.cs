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
    internal class NextPageButton : Button
    {
        protected override async void OnClick()
        {
            // Pull module settings
            Dictionary<string, string> settings = MapSeriesTools.Current.Settings;

            LayoutProjectItem lytItem = Project.Current.GetItems<LayoutProjectItem>()
                         .FirstOrDefault(item => item.Name.Contains(settings["SelectedMapSeries"]));

            await QueuedTask.Run(() =>
            {
                // Get layout
                Layout map_series_layout = lytItem.GetLayout();
                MapSeries MS = map_series_layout.MapSeries as MapSeries;

                // DEBUG: show selected map series
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Selected layout {lytItem.Name}");
                if (MS != null)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Layout map series: {MS}");
                }
                else
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Layout has to map series!");
                }

                // Set current page to next page 
                //MS.SetCurrentPageNumber(MS.NextPageNumber);

            });
        }
    }
}
