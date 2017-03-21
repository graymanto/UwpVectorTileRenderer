using Mapsui.Layers;
using System.Collections.Generic;
using System.Linq;
using Mapsui.Geometries;
using Mapsui.Providers;
using BruTile;
using BruTile.Cache;
using Mapsui.Rendering;

namespace VectorTilesRenderer
{
    public class VectorTileLayer : BaseLayer
    {
        private readonly TileFetcher _fetcher = new TileFetcher();
        private readonly IAsyncTileSource _tileSource;
        private readonly IRenderGetStrategy _renderFetchStrategy = new RenderGetStrategy();
        private readonly MemoryCache<Feature> _memoryCache;

        public VectorTileLayer(IAsyncTileSource tileSource)
        {
            _tileSource = tileSource;
            _memoryCache = new MemoryCache<Feature>(0, 10000);

            _fetcher.DataChanged += (s, ea) => OnDataChanged(ea);
        }

        public override BoundingBox Envelope
        {
            get
            {
                return _tileSource.Schema == null ? null : _tileSource.Schema.Extent.ToBoundingBox();
            }
        }

        public override void AbortFetch()
        {
        }

        public override void ClearCache()
        {
            _memoryCache.Clear();
        }

        public override IEnumerable<IFeature> GetFeaturesInView(BoundingBox box, double resolution)
        {
            if (_tileSource.Schema == null) return Enumerable.Empty<IFeature>();
            return _renderFetchStrategy.GetFeatures(box, resolution, _tileSource.Schema, _memoryCache);
        }

        public override void ViewChanged(bool majorChange, BoundingBox extent, double resolution)
        {
            if (Enabled && extent.GetArea() > 0 && MaxVisible > resolution && MinVisible < resolution)
            {
                _fetcher.LoadTilesForArea(_tileSource, _memoryCache, extent, resolution);
            }
        }
    }
}
