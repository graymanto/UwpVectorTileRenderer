using Mapsui;

namespace VectorTilesRenderer
{
    public class ViewChangedEventArgs
    {
        public IViewport Viewport { get; set; }
        public bool UserAction
        {
            get; set;
        }
    }
}
