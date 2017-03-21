using Mapsui.Layers;
using System.Linq;
using VectorTilesRenderer.Extensions;
using Mapsui;
using Microsoft.Graphics.Canvas;
using Mapbox.Vector.Tile;
using System.IO;
using System.Diagnostics;
using GeoJSON.Net;
using GeoJSON.Net.Geometry;
using Microsoft.Graphics.Canvas.Geometry;
using MbTileLayer = Mapbox.Vector.Tile.VectorTileLayer;
using GeoJSON.Net.Feature;
using Windows.UI;

namespace VectorTilesRenderer
{
    class Win2DRenderer
    {
        public static void Render(RenderContext context)
        {
            context.Layers
                .Where(l => l.Enabled)
                .ForEach(l => RenderLayer(context.ViewPort, l, context.DrawingSession));

        }

        private static void RenderLayer(IViewport viewPort, ILayer layer, CanvasDrawingSession drawingSession)
        {
            var features = layer.GetFeaturesInView(viewPort.Extent, viewPort.RenderResolution);
            features
                .Where(f => f.Geometry is VectorTile)
                .ForEach(f => RenderVectorTile(drawingSession, viewPort, f.Geometry as VectorTile));
        }

        private static void RenderVectorTile(CanvasDrawingSession session, IViewport viewport, VectorTile tile)
        {
            if (tile.ParsedTile == null)
            {
                var parsedTile = VectorTileParser.Parse(new MemoryStream(tile.TileData));
                tile.ParsedTile = parsedTile.AsEnumerable();
            }

            tile.ParsedTile.OrderBy((f) =>
            {
                if (f.Name == "water")
                    return 1;
                if (f.Name == "earth")
                    return 2;
                return 3;

            })
            .ForEach(t => RenderTileLayer(session, viewport, t, t.ToGeoJSON(tile.Column, tile.Row, tile.Level)));
        }

        private static void RenderTileLayer(CanvasDrawingSession session, IViewport viewport, MbTileLayer layer,
            FeatureCollection features)
        {
            var layerName = layer.Name;

            features
                .Features
                .ForEach(f =>
                {
                    if (f.Geometry.Type == GeoJSONObjectType.Polygon)
                        RenderPolygon(session, viewport, layer, f);
                    else if (f.Geometry.Type == GeoJSONObjectType.MultiPolygon)
                        RenderMultiPolygon(session, viewport, layer, f);
                    else if (f.Geometry.Type == GeoJSONObjectType.Point)
                        RenderPoint(session, viewport, layer, f);
                    else if (f.Geometry.Type == GeoJSONObjectType.LineString)
                        RenderLineString(session, viewport, layer, f);
                    else if (f.Geometry.Type == GeoJSONObjectType.MultiLineString)
                        RenderMultiLineString(session, viewport, layer, f);
                    else
                        Debug.WriteLine("Unknown geometry type " + f.Geometry.Type);
                });
        }

        private static Color GetFillColorForType(string type)
        {
            if (type == "water")
                return Color.FromArgb(255, 191, 217, 242);
            if (type == "earth")
                return Color.FromArgb(125, 205, 242, 191);
            if (type == "boundaries")
                return Color.FromArgb(200, 216, 219, 214);
            if (type == "roads")
                return Color.FromArgb(100, 95, 102, 92);

            return Colors.Transparent;
        }

        private static CanvasStrokeStyle GetStrokeStyleForType(string type)
        {
            var strokeStyle = new CanvasStrokeStyle();

            if (type == "boundaries")
            {
                strokeStyle.DashStyle = CanvasDashStyle.Dash;
            }

            return strokeStyle;
        }

        private static void RenderLineString(CanvasDrawingSession session, IViewport viewport, MbTileLayer layer,
            Feature feature)
        {
            if (layer.Name == "water")
                return;

            var lineString = feature.Geometry as LineString;

            var points = lineString
                .Coordinates
                .AsScreenPositions(viewport);

            var geometry = CanvasGeometry.CreatePolygon(session.Device, points.ToArray());
            session.DrawGeometry(geometry, GetFillColorForType(layer.Name), 1, GetStrokeStyleForType(layer.Name));
        }

        private static void RenderMultiLineString(CanvasDrawingSession session, IViewport viewport, MbTileLayer layer,
            Feature feature)
        {
            if (layer.Name == "water")
                return;

            var lineString = feature.Geometry as MultiLineString;

            lineString
                .Coordinates
                .ForEach(ls =>
               {
                   var points = ls.Coordinates
                    .AsScreenPositions(viewport);

                   var geometry = CanvasGeometry.CreatePolygon(session.Device, points.ToArray());
                   session.DrawGeometry(geometry, GetFillColorForType(layer.Name), 1, GetStrokeStyleForType(layer.Name));
               });
        }

        private static void RenderPolygon(CanvasDrawingSession session, IViewport viewport, MbTileLayer layer,
            Feature feature)
        {
            var polygon = feature.Geometry as Polygon;

            polygon
                .Coordinates
                .ForEach(c =>
                {
                    var points = c.Coordinates.AsScreenPositions(viewport);

                    var geometry = CanvasGeometry.CreatePolygon(session.Device, points.ToArray());
                    session.FillGeometry(geometry, GetFillColorForType(layer.Name));
                });
        }

        private static void RenderMultiPolygon(CanvasDrawingSession session, IViewport viewport, MbTileLayer layer,
            Feature feature)
        {
            var polygon = feature.Geometry as MultiPolygon;

            polygon
                .Coordinates
                .ForEach(outer =>
                {
                    outer.Coordinates
                    .ForEach(c =>
                   {
                       var points = c.Coordinates.AsScreenPositions(viewport);

                       var geometry = CanvasGeometry.CreatePolygon(session.Device, points.ToArray());
                       session.FillGeometry(geometry, GetFillColorForType(layer.Name));
                   });
                });
        }

        private static void RenderPoint(CanvasDrawingSession session, IViewport viewport, MbTileLayer layer,
            Feature feature)
        {
            var point = feature.Geometry as Point;
            var gp = (GeographicPosition)point.Coordinates;
            var mappedPoint = gp.AsScreenPosition(viewport);

            session.DrawCircle(mappedPoint, 2, GetFillColorForType(layer.Name));
        }
    }
}
