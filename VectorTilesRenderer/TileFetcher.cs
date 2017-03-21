using BruTile;
using BruTile.Cache;
using Mapsui.Geometries;
using Mapsui.Providers;
using System.Collections.Generic;
using System.Linq;
using BoundingBox = Mapsui.Geometries.BoundingBox;
using VectorTilesRenderer.Extensions;
using System.Threading.Tasks;
using System.Diagnostics;
using System;
using Mapsui.Fetcher;

namespace VectorTilesRenderer
{
    public class TileFetcher
    {
        private int _concurrentFetches = 10;
        //private int _maxRetries = 3;

        public event EventHandler<DataChangedEventArgs> DataChanged;

        public void AbortAllFetches()
        {
            // TODO: implement cancellation tokens.
        }

        public void LoadTilesForArea(IAsyncTileSource tileSource, MemoryCache<Feature> cache,
            BoundingBox extent, double resolution)
        {
            // TODO: limit this to 1 thread
            Task.Factory.StartNew(() =>
            {
                var nearestLevel = Utilities.GetNearestLevel(tileSource.Schema.Resolutions, resolution);
                var requiredTiles = GetRequiredTiles(tileSource.Schema, extent.ToExtent(), nearestLevel);
                var toFetch = requiredTiles
                    .Where(t => cache.Find(t.Index) == null);

                toFetch
                    .Batch(_concurrentFetches)
                    .Select(fetchGroup => fetchGroup.Select(tile => CreateTileDownloadTask(tile, tileSource)))
                    .Select(fetchGroup => Task.WhenAll(fetchGroup))
                    .SelectMany(r => r.Result)
                    .Where(r => r.Item2 != null)
                    .ForEach(item => 
                        {
                            var feature = CreateVectorTileFeature(item.Item2, item.Item1.Index);
                            cache.Add(item.Item1.Index, feature);
                        });

                return toFetch.FirstOrDefault();

            }, TaskCreationOptions.LongRunning)
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                    Debug.WriteLine("Failure to download tiles", task.Exception.Flatten());
                else
                {
                    Debug.WriteLine("Downloading tiles complete");

                    // Arbitrarily return first fetched as data that's updated
                }
            });
        }

        private Task<Tuple<TileInfo, byte[]>> CreateTileDownloadTask(TileInfo tileInfo, IAsyncTileSource tileSource)
        {
            // TODO: retries
            var tcs = new TaskCompletionSource<Tuple<TileInfo, byte[]>>();
            tileSource.GetTileAsync(tileInfo)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        //tcs.SetException(t.Exception);
                        tcs.SetResult(new Tuple<TileInfo, byte[]>(tileInfo, null));
                    else
                    {
                        tcs.SetResult(new Tuple<TileInfo, byte[]>(tileInfo, t.Result));
                        DataChanged?.Invoke(this, new DataChangedEventArgs(t.Exception, t.IsCanceled, tileInfo));
                    }
                });

            return tcs.Task;
        }

        private Feature CreateVectorTileFeature(byte[] tileData, TileIndex index)
        {
            return new Feature
            {
                Geometry = new VectorTile(tileData)
                {
                    Row = index.Row,
                    Column = index.Col,
                    Level = int.Parse(index.Level),
                },
            };
        }

        private IEnumerable<TileInfo> GetRequiredTiles(ITileSchema schema, Extent extent, string levelId)
        {
            var resolution = schema.Resolutions[levelId].UnitsPerPixel;

            return schema.Resolutions
                .Where(k => k.Value.UnitsPerPixel >= resolution)
                .OrderBy(x => x.Value.UnitsPerPixel)
                .SelectMany(l => schema.GetOrderedTileInfo(extent, levelId))
                .Where(i => i.Index.Row >= 0 && i.Index.Col >= 0)
                .Select(i => i);
        }
    }
}

