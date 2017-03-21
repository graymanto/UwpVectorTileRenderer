using BruTile;
using BruTile.Predefined;
using Mapsui.Geometries;
using System.IO;
using VectorTilesRenderer;
using VectorTilesRenderer.Cache;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace VectorTilesRendererExample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            var cacheLocation = Path.Combine(Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path, "JsonTiles");

            var tileSource = new HttpVectorTileSource(
                CreateSchema(),
                "https://tile.mapzen.com/mapzen/vector/v1/all/{z}/{x}/{y}.mvt?api_key=mapzen-{key-here}",
                new DiskCache(cacheLocation)
                )
            {
                Name = "Vector tile"
            };

            var tileLayer = new VectorTileLayer(tileSource);

            MapControl.Map.Layers.Add(tileLayer);
        }

        private static ITileSchema CreateSchema()
        {
            return new GlobalSphericalMercator();

            //var minLong = 4.1061;
            //var minLat = 44.318;
            //var maxLong = 7.2647;
            //var maxLat = 46.037;
            //var startPoint = new BoundingBox(minLong, minLat, maxLong, maxLat);
            //var schema = new GlobalSphericalMercator(minZoom, maxZoom);
            //schema.Extent = MapUtils.ToMercator(new Extent(startPoint.MinX, startPoint.MinY,
            //    startPoint.MaxX, startPoint.MaxY));
            //return schema;
        }
    }
}
