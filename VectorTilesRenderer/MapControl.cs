﻿// MapControl based on examples from Mapsui library but ammended to render to win2d

using Mapsui;
using Mapsui.Fetcher;
using Mapsui.Geometries;
using Mapsui.Layers;
using Mapsui.UI.Uwp;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;
using WPoint = Windows.Foundation.Point;

namespace VectorTilesRenderer
{
    public class MapControl : Grid, IDisposable
    {
        private Map _map;
        private string _errorMessage;
        private readonly DoubleAnimation _zoomAnimation = new DoubleAnimation();
        private readonly Storyboard _zoomStoryBoard = new Storyboard();
        private bool _viewportInitialized;
        private readonly Rectangle _bboxRect;
        private readonly Canvas _renderTarget;
        private readonly CanvasControl _overlayRenderTarget;
        private WPoint _previousPosition;
        private SynchronizationContext _mainThread;

        public event EventHandler ErrorMessageChanged;
        public event EventHandler<ViewChangedEventArgs> ViewChanged;

        public bool ZoomToBoxMode { get; set; }
        public IViewport Viewport => Map.Viewport;
        public bool AllowPanPastEdges { get; set; }

        public Map Map
        {
            get
            {
                return _map;
            }
            set
            {
                if (_map != null)
                {
                    var temp = _map;
                    _map = null;
                    temp.PropertyChanged -= MapPropertyChanged;
                    temp.Dispose();
                }

                _map = value;
                //all changes of all layers are returned through this event handler on the map
                if (_map != null)
                {
                    _map.DataChanged += MapDataChanged;
                    _map.PropertyChanged += MapPropertyChanged;
                    _map.ViewChanged(true);
                }
                OnViewChanged();
                RefreshGraphics();
            }
        }

        void MapPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Envelope")
            {
                InitializeViewport();
                _map.ViewChanged(true);
            }
        }

        public string ErrorMessage => _errorMessage;

        public bool ZoomLocked { get; set; }

        private void SetupRenderingOverlay()
        {
            _overlayRenderTarget.CompositeMode = ElementCompositeMode.Inherit;
            _overlayRenderTarget.Draw += (sender, args) =>
            {
                var context = new RenderContext
                {
                    DrawingSession = args.DrawingSession,
                    Layers = Map.Layers.Where(l => l is VectorTileLayer),
                    ViewPort = Map.Viewport,
                };

                Win2DRenderer.Render(context);
            };
        }

        public MapControl()
        {
            _mainThread = SynchronizationContext.Current;

            _renderTarget = new Canvas
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Colors.Transparent),
            };

            _overlayRenderTarget = new CanvasControl
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Colors.Red),
            };

            _renderTarget.SizeChanged += (o, e) =>
            {
                Map.Viewport.Height = e.NewSize.Height;
                Map.Viewport.Width = e.NewSize.Width;
            };

            SetupRenderingOverlay();

            Children.Add(_overlayRenderTarget);
            Children.Add(_renderTarget);

            Unloaded += (sender, args) =>
            {
                _overlayRenderTarget.RemoveFromVisualTree();
            };

            _bboxRect = new Rectangle
            {
                StrokeDashArray = new DoubleCollection { 3.0 },
                Opacity = 0.3,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                Visibility = Visibility.Collapsed
            };
            Children.Add(_bboxRect);

            Map = new Map();
            Loaded += MapControlLoaded;

            SizeChanged += MapControlSizeChanged;
            PointerWheelChanged += OnPointerWheelChange;

            ManipulationMode = ManipulationModes.Scale | ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            ManipulationDelta += OnManipulationDelta;
            ManipulationCompleted += OnManipulationCompleted;
            ManipulationInertiaStarting += OnManipulationInertiaStarting;

            var orientationSensor = SimpleOrientationSensor.GetDefault();
            if (orientationSensor != null)
            {
                onSensorChange = (s, e) =>
                {
                    orientationSensor.OrientationChanged += (sender, args) =>
                        Task.Run(() => Dispatcher.RunAsync(CoreDispatcherPriority.Normal, Refresh)).ConfigureAwait(false);
                };

                orientationSensor.OrientationChanged += onSensorChange;
            }
        }

        TypedEventHandler<SimpleOrientationSensor, SimpleOrientationSensorOrientationChangedEventArgs> onSensorChange;

        void OnPointerWheelChange(object sender, PointerRoutedEventArgs e)
        {
            if (ZoomLocked) return;
            if (!_viewportInitialized) return;

            var currentPoint = e.GetCurrentPoint(this); //Needed for both MouseMove and MouseWheel event for mousewheel event

            var mousePosition = new Mapsui.Geometries.Point(currentPoint.RawPosition.X, currentPoint.RawPosition.Y);

            var newResolution = DetermineNewResolution(currentPoint.Properties.MouseWheelDelta, Map.Viewport.Resolution);

            // 1) Temporarily center on the mouse position
            Map.Viewport.Center = Map.Viewport.ScreenToWorld(mousePosition.X, mousePosition.Y);

            // 2) Then zoom 
            Map.Viewport.Resolution = newResolution;

            // 3) Then move the temporary center of the map back to the mouse position
            Map.Viewport.Center = Map.Viewport.ScreenToWorld(
              Map.Viewport.Width - mousePosition.X,
              Map.Viewport.Height - mousePosition.Y);

            e.Handled = true;

            _map.ViewChanged(true);
            OnViewChanged(true);
        }

        private double DetermineNewResolution(int mouseWheelDelta, double currentResolution)
        {
            if (mouseWheelDelta > 0) return ZoomHelper.ZoomIn(_map.Resolutions, currentResolution);
            if (mouseWheelDelta < 0) return ZoomHelper.ZoomOut(_map.Resolutions, currentResolution);
            return currentResolution;
        }

        private void OnViewChanged(bool userAction = false)
        {
            if (_map != null)
            {
                _overlayRenderTarget.Invalidate();
                ViewChanged?.Invoke(this, new ViewChangedEventArgs { Viewport = Map.Viewport, UserAction = userAction });
            }
        }

        public void Refresh()
        {
            _map.ViewChanged(true);
            RefreshGraphics();
        }

        private void RefreshGraphics()
        {
            InvalidateArrange();
            InvalidateMeasure();
            _renderTarget.InvalidateArrange();
            _renderTarget.InvalidateMeasure();
            _overlayRenderTarget.Invalidate();
            _overlayRenderTarget.InvalidateArrange();
            _overlayRenderTarget.InvalidateMeasure();
        }

        public void Clear()
        {
            _map?.ClearCache();
            RefreshGraphics();
        }

        public void ZoomIn()
        {
            if (ZoomLocked) return;
            if (!_viewportInitialized) return;

            Map.Viewport.Resolution = ZoomHelper.ZoomIn(_map.Resolutions, Map.Viewport.Resolution);

            OnViewChanged();
        }

        public void ZoomOut()
        {
            if (ZoomLocked) return;
            if (!_viewportInitialized) return;

            Map.Viewport.Resolution = ZoomHelper.ZoomOut(_map.Resolutions, Map.Viewport.Resolution);

            OnViewChanged();
        }

        protected void OnErrorMessageChanged(EventArgs e)
        {
            ErrorMessageChanged?.Invoke(this, e);
        }

        private void MapControlLoaded(object sender, RoutedEventArgs e)
        {
            if (!_viewportInitialized) InitializeViewport();
            UpdateSize();
            InitAnimation();
        }

        private void InitAnimation()
        {
            _zoomAnimation.Duration = new Duration(new TimeSpan(0, 0, 0, 0, 1000));
            _zoomAnimation.EasingFunction = new QuarticEase();
            Storyboard.SetTarget(_zoomAnimation, this);
            Storyboard.SetTargetProperty(_zoomAnimation, "Resolution");
            _zoomStoryBoard.Children.Add(_zoomAnimation);
        }

        private void MapControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_viewportInitialized) InitializeViewport();
            Clip = new RectangleGeometry { Rect = new Rect(0, 0, ActualWidth, ActualHeight) };
            UpdateSize();
            _map.ViewChanged(true);
            OnViewChanged();
            Refresh();
        }

        private void UpdateSize()
        {
            if (Viewport == null) return;
            Map.Viewport.Width = ActualWidth;
            Map.Viewport.Height = ActualHeight;
        }

        public void MapDataChanged(object sender, DataChangedEventArgs e)
        {
            if (!Dispatcher.HasThreadAccess)
            {
                Task.Run(
                    () => Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => MapDataChanged(sender, e)))
                    .ConfigureAwait(false);
            }
            else
            {
                if (e.Cancelled)
                {
                    _errorMessage = "Cancelled";
                    OnErrorMessageChanged(EventArgs.Empty);
                }
                else if (e.Error is System.Net.WebException)
                {
                    _errorMessage = "WebException: " + e.Error.Message;
                    OnErrorMessageChanged(EventArgs.Empty);
                }
                else if (e.Error != null)
                {
                    _errorMessage = e.Error.GetType() + ": " + e.Error.Message;
                    OnErrorMessageChanged(EventArgs.Empty);
                }
                else // no problems
                {
                    RefreshGraphics();
                }
            }
        }

        private void InitializeViewport()
        {
            if (ActualWidth.IsNanOrZero()) return;
            if (_map == null) return;
            if (_map.Envelope == null) return;
            if (_map.Envelope.Width.IsNanOrZero()) return;
            if (_map.Envelope.Height.IsNanOrZero()) return;
            if (_map.Envelope.GetCentroid() == null) return;

            if (double.IsNaN(Map.Viewport.Resolution))
                Map.Viewport.Resolution = _map.Envelope.Width / ActualWidth;
            if (double.IsNaN(Map.Viewport.Center.X) || double.IsNaN(Map.Viewport.Center.Y))
                Map.Viewport.Center = _map.Envelope.GetCentroid();

            _viewportInitialized = true;
        }

        public void ZoomToBox(Mapsui.Geometries.Point beginPoint, Mapsui.Geometries.Point endPoint)
        {
            double x, y, resolution;
            var width = Math.Abs(endPoint.X - beginPoint.X);
            var height = Math.Abs(endPoint.Y - beginPoint.Y);
            if (width <= 0) return;
            if (height <= 0) return;

            ZoomHelper.ZoomToBoudingbox(beginPoint.X, beginPoint.Y, endPoint.X, endPoint.Y, ActualWidth, out x, out y, out resolution);
            resolution = ZoomHelper.ClipToExtremes(_map.Resolutions, resolution);

            Map.Viewport.Center = new Mapsui.Geometries.Point(x, y);
            Map.Viewport.Resolution = resolution;

            _map.ViewChanged(true);
            OnViewChanged();
            RefreshGraphics();
            ClearBBoxDrawing();
        }

        private void ClearBBoxDrawing()
        {
            _bboxRect.Margin = new Thickness(0, 0, 0, 0);
            _bboxRect.Width = 0;
            _bboxRect.Height = 0;
        }

        public void ZoomToFullEnvelope()
        {
            if (Map.Envelope == null) return;
            if (ActualWidth.IsNanOrZero()) return;
            Map.Viewport.Resolution = Map.Envelope.Width / ActualWidth;
            Map.Viewport.Center = Map.Envelope.GetCentroid();

            OnViewChanged();
        }

        private static void OnManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
        {
            e.TranslationBehavior.DesiredDeceleration = 25 * 96.0 / (1000.0 * 1000.0);
        }

        public BoundingBox GetNewViewportExtent(double screenX, double screenY, double previousScreenX, double previousScreenY)
        {
            var previous = Map.Viewport.ScreenToWorld(previousScreenX, previousScreenY);
            var current = Map.Viewport.ScreenToWorld(screenX, screenY);

            var newLeft = Map.Viewport.Extent.Left + previous.X - current.X;
            var newRight = Map.Viewport.Extent.Right + previous.X - current.X;
            var newTop = Map.Viewport.Extent.Top + previous.Y - current.Y;
            var newBottom = Map.Viewport.Extent.Bottom - previous.Y + current.Y;

            return new BoundingBox(newLeft, newBottom, newRight, newTop);
        }

        private void OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (_previousPosition == default(WPoint) || double.IsNaN(_previousPosition.X))
            {
                _previousPosition = e.Position;
                return;
            }

            if (Distance(e.Position.X, e.Position.Y, _previousPosition.X, _previousPosition.Y) > 50
                && Math.Sqrt(Math.Pow(e.Velocities.Linear.X, 2.0) + Math.Pow(e.Velocities.Linear.Y, 2.0)) < 1)
            {
                _previousPosition = default(WPoint);
                return;
            }

            var newExtent = GetNewViewportExtent(e.Position.X, e.Position.Y, _previousPosition.X, _previousPosition.Y);

            var zero = Map.Viewport.ScreenToWorld(0, 0);

            var movingRight = e.Position.X > _previousPosition.X;
            var movingUp = e.Position.Y > _previousPosition.Y;

            if (AllowPanPastEdges)
            {
                Map.Viewport.Transform(e.Position.X, e.Position.Y, _previousPosition.X, _previousPosition.Y, e.Delta.Scale);
            }
            else
            {
                var stopDragX = newExtent.Left < Map.Envelope.Left && movingRight
                    || newExtent.Right > Map.Envelope.Right && !movingRight;

                var stopDragY = newExtent.Top > Map.Envelope.Top && movingUp
                    || newExtent.Bottom < Map.Envelope.Bottom && !movingUp;

                if (!(stopDragX && stopDragY))
                {
                    var newX = !stopDragX ? e.Position.X : _previousPosition.X;
                    var newY = !stopDragY ? e.Position.Y : _previousPosition.Y;

                    Map.Viewport.Transform(newX, newY, _previousPosition.X, _previousPosition.Y, e.Delta.Scale);
                }
            }

            _previousPosition = e.Position;

            OnViewChanged(true);
        }

        public static double Distance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x1 - x2, 2.0) + Math.Pow(y1 - y2, 2.0));
        }

        private void OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            _previousPosition = default(WPoint);
            Refresh();
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    var orientationSensor = SimpleOrientationSensor.GetDefault();
                    if (orientationSensor != null)
                    {
                        orientationSensor.OrientationChanged -= onSensorChange;
                    }

                    _overlayRenderTarget.RemoveFromVisualTree();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
