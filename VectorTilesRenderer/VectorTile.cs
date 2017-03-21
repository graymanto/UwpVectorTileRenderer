using System;
using Mapsui.Geometries;
using System.Collections.Generic;
using MbTileLayer = Mapbox.Vector.Tile.VectorTileLayer;

namespace VectorTilesRenderer
{
    public class VectorTile : IVectorTile
    {
        private readonly byte[] _tileData;

        public VectorTile(byte[] tileData)
        {
            _tileData = tileData;
        }

        public int Row { get; set; }
        public int Column { get; set; }
        public int Level { get; set; }

        public IEnumerable<MbTileLayer> ParsedTile { get; set; }

        public byte[] TileData
        {
            get { return _tileData; }
        }

        public byte[] AsBinary()
        {
            return _tileData;
        }

        public string AsText()
        {
            throw new NotImplementedException();
        }

        public Geometry Clone()
        {
            throw new NotImplementedException();
        }

        public bool Contains(Point point)
        {
            throw new NotImplementedException();
        }

        public double Distance(Point point)
        {
            throw new NotImplementedException();
        }

        public Geometry Envelope()
        {
            throw new NotImplementedException();
        }

        public bool Equals(Geometry geom)
        {
            throw new NotImplementedException();
        }

        public BoundingBox GetBoundingBox()
        {
            throw new NotImplementedException();
        }

        public bool IsEmpty()
        {
            throw new NotImplementedException();
        }

        public bool Touches(Point point, double marginX = 0, double marginY = 0)
        {
            throw new NotImplementedException();
        }
    }
}
