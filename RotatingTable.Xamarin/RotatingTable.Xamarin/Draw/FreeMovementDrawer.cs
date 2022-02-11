using RotatingTable.Xamarin.Services;
using RotatingTable.Xamarin.TouchTracking;
using RotatingTable.Xamarin.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace RotatingTable.Xamarin.Draw
{
    public class FreeMovementDrawer : BaseDrawer
    {
        private bool _isDragging = false;
        private int _startAngle;
        private int _endAngle;

        public FreeMovementDrawer(SKCanvasView canvasView, MainModel model) : base(canvasView, model)
        {
        }

        public override void Draw(SKPaintSurfaceEventArgs args)
        {
            base.Draw(args);

            DrawCircle();
            DrawSector();
            DrawBorder();
        }

        protected void DrawSector()
        {
            if (!_isDragging)
                return;

            var path = new SKPath();
            path.MoveTo(0, 0);
            var angle = _endAngle - _startAngle;
            if (angle == 0)
                angle = 1;

            path.ArcTo(Rect, _startAngle, angle, false);
            path.LineTo(0, 0);

            _paint.Style = SKPaintStyle.Fill;
            _paint.Shader = null;
            _paint.Color = ((Color)Application.Current.Resources["Highlight"]).ToSKColor();
            Canvas.DrawPath(path, _paint);
        }

        public override async void OnTouchEffectAction(object sender, TouchActionEventArgs args)
        {
            var pt = Transform(args.Location);
            switch (args.Type)
            {
                case TouchActionType.Pressed:
                    if (!IsPtInsideCircle(pt))
                    {
                        Clear();
                        break;
                    }

                    _startAngle = _endAngle = (int)PtToAngle(pt);
                    _isDragging = true;
                    CanvasView.InvalidateSurface();
                    break;

                case TouchActionType.Moved:
                    if (!_isDragging)
                        break;

                    if (!IsPtInsideCircle(pt))
                        Clear();
                    else
                    {
                        _endAngle = (int)PtToAngle(pt);
                        if (Math.Abs(_endAngle - _startAngle) > 180)
                        {
                            if (_startAngle > 180)
                                _endAngle += 360;
                            else
                                _endAngle -= 360;
                        }
                    }
                    CanvasView.InvalidateSurface();
                    break;

                case TouchActionType.Released:
                    await StartMovementAsync();
                    break;

                default:
                    CanvasView.InvalidateSurface();
                    Clear();
                    break;
            }
        }

        private async Task<bool> StartMovementAsync()
        {
            var angle = _endAngle - _startAngle;
            if (angle == 0)
                return false;

            var service = DependencyService.Resolve<IBluetoothService>();
            await service.WriteAsync($"FM {angle}");
            var response = await service.ReadAsync();
            Model.IsRunning = response == "OK";
            //service.BeginListening((s, a) => Model.OnDataReseived(a.Text));
            return true;
        }

        private SKPoint Transform(Point pt)
        {
            var point = new SKPoint((float)(CanvasView.CanvasSize.Width * pt.X / CanvasView.Width),
                               (float)(CanvasView.CanvasSize.Height * pt.Y / CanvasView.Height));
            point.Offset(-Center.X, -Center.Y);
            return point;
        }

        private bool IsPtInsideCircle(SKPoint pt)
        {
            var hypotenuse = Math.Sqrt(pt.X * pt.X + pt.Y * pt.Y);
            return hypotenuse < Radius;
        }

        private void Clear()
        {
            _isDragging = false;
            _startAngle = _endAngle = 0;
        }

        private double PtToAngle(SKPoint pt)
        {
            SKPoint start = new SKPoint(Radius, 0);
            double cos = (pt.X * start.X + pt.Y * start.Y) /
                (Math.Sqrt(pt.X * pt.X + pt.Y * pt.Y) * Math.Sqrt(start.X * start.X + start.Y * start.Y));
            double radians = Math.Acos(cos);

            if (pt.Y < 0)
            {
                if (pt.X < 0)
                    radians = 2 * Math.PI - radians;
                else
                    radians = 3 * Math.PI / 2 + (Math.PI / 2 - radians);
            }

            return ToDegrees(radians);
        }

        private double ToDegrees(double angle) => angle * 180 / Math.PI;
    }
}
