using Mapsui;
using Mapsui.Layers;
using Microsoft.Graphics.Canvas;
using System.Collections.Generic;

namespace VectorTilesRenderer
{
    public class RenderContext
    {
        public CanvasDrawingSession DrawingSession { get; set; }
        public IEnumerable<ILayer> Layers { get; set; }
        public IViewport ViewPort { get; set; }
    }
}
