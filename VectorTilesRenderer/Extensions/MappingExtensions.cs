using GeoJSON.Net.Geometry;
using Mapsui;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace VectorTilesRenderer.Extensions
{
    public static class MappingExtensions
    {
        public static IEnumerable<Vector2> AsScreenPositions(this IEnumerable<IPosition> positions, IViewport viewport)
        {
            return positions
                .Select(p => (GeographicPosition)p)
                .Select(p => p.AsScreenPosition(viewport));
        }

        public static Vector2 AsScreenPosition(this GeographicPosition position, IViewport viewport)
        {
            var worldPosition = MapUtils.FromLonLat(position.Longitude, position.Latitude);
            return MapUtils.WorldToScreen(worldPosition, viewport);
        }
    }
}
