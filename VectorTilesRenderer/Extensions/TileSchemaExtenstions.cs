using BruTile;
using Mapsui.Geometries.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace VectorTilesRenderer.Extensions
{
    public static class TileSchemaExtensions
    {
        public static IEnumerable<TileInfo> GetOrderedTileInfo(this ITileSchema schema, Extent extent, string levelId)
        {
            return schema.GetTileInfos(extent, levelId)
                .OrderBy(
                        t => Algorithms.Distance(extent.CenterX, extent.CenterY, t.Extent.CenterX, t.Extent.CenterY));
        }
    }
}
