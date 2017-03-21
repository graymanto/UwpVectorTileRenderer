using BruTile;
using Mapsui;
using System;
using System.Numerics;

namespace VectorTilesRenderer
{
    public static class MapUtils
    {
        private const double Radius = 6378137;
        private const double D2R = Math.PI / 180;
        private const double HalfPi = Math.PI / 2;

        public static Extent ToMercator(Extent extent)
        {
            var minX = extent.MinX;
            var minY = extent.MinY;
            var minValues = ToMercator(minX, minY);
            var maxX = extent.MaxX;
            var maxY = extent.MaxY;
            var maxValues = ToMercator(maxX, maxY);
            return new Extent(minValues.Item1, minValues.Item2, maxValues.Item1, maxValues.Item2);
        }

        public static Tuple<double, double> ToMercator(double lon, double lat)
        {
            if ((Math.Abs(lon) > 180 || Math.Abs(lat) > 90))
                return new Tuple<double, double>(-180, -90);
            double num = lon * 0.017453292519943295;
            double finalLon = Radius * num; 
            double a = lat * 0.017453292519943295; // 1 degree in radians.
            var finalLat = 3189068.5 * Math.Log((1.0 + Math.Sin(a)) / (1.0 - Math.Sin(a)));
            return new Tuple<double, double>(finalLon, finalLat);
        }

        public static Vector2 WorldToScreen(Vector2 input, IViewport viewport)
        {
            var screenCenterX = viewport.Width / 2.0;
            var screenCenterY = viewport.Height / 2.0;
            var screenX = (input.X - viewport.Center.X) / viewport.Resolution + screenCenterX;
            var screenY = (viewport.Center.Y - input.Y) / viewport.Resolution + screenCenterY;

            return new Vector2((float)screenX, (float)screenY);
        }

        public static Vector2 FromLonLat(double lon, double lat)
        {
            var lonRadians = (D2R * lon);
            var latRadians = (D2R * lat);

            var x = Radius * lonRadians;
            var y = Radius * Math.Log(Math.Tan(Math.PI * 0.25 + latRadians * 0.5));

            return new Vector2((float)x, (float)y);
        }

        public static Vector2 ToLonLat(double x, double y)
        {
            var ts = Math.Exp(-y / (Radius));
            var latRadians = HalfPi - 2 * Math.Atan(ts);

            var lonRadians = x / (Radius);

            var lon = (lonRadians / D2R);
            var lat = (latRadians / D2R);

            return new Vector2((float)lon, (float)lat);
        }
    }
}
