using Mapsui.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorTilesRenderer
{
    public interface IVectorTile : IGeometry
    {
        byte[] TileData { get; }
    }
}
