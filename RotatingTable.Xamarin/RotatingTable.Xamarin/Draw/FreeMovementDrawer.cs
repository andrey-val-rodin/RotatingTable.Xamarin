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
        private int _oldStartAngle;
        private int _endAngle;

        public FreeMovementDrawer(SKCanvasView canvasView, MainModel model) : base(canvasView, model)
        {
        }

        private int Angle
        {
            get => _endAngle - _startAngle;
        }

        public override void Draw(SKPaintSurfaceEventArgs args)
        {
            base.Draw(args);

            DrawCircle();
            DrawSelectedSector();
            DrawArrow();
            if (_isDragging)
                DrawText(-Radius, -Radius + 50, $"{Angle}°", 40, SKTextAlign.Left);
            DrawBorder();
        }

        private void DrawSelectedSector()
        {
            if (Angle == 0)
                return;

            DrawSector(_startAngle, Angle);
        }

        private void DrawArrow()
        {
            const int indent = 24;

            if (Math.Abs(Angle) < 8)
                return;

            var path = new SKPath();
            var rect = Rect;
            rect.Inflate(-indent, -indent);
            path.ArcTo(rect, _startAngle, Angle, true);

            // Draw arc
            _paint.Style = SKPaintStyle.Stroke;
            _paint.Shader = null;
            _paint.Color = ((Color)Application.Current.Resources["Arrow"]).ToSKColor();
            Canvas.DrawPath(path, _paint);

            // Draw arrow
            _paint.Style = SKPaintStyle.Fill;
            path = new SKPath();
            if (Angle > 0)
            {
                path.MoveTo(GetCirclePt(_endAngle, Radius - indent));
                path.LineTo(GetCirclePt(_endAngle - 6, Radius - indent + 4));
                path.LineTo(GetCirclePt(_endAngle - 6, Radius - indent - 4));
                path.Close();
                Canvas.DrawPath(path, _paint);
            }
            else
            {
                path.MoveTo(GetCirclePt(_endAngle, Radius - indent));
                path.LineTo(GetCirclePt(_endAngle + 6, Radius - indent + 4));
                path.LineTo(GetCirclePt(_endAngle + 6, Radius - indent - 4));
                path.Close();
                Canvas.DrawPath(path, _paint);
            }
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
                    _isDragging = false;
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
            if (Angle == 0)
                return false;

            var service = DependencyService.Resolve<IBluetoothService>();
            await service.WriteAsync($"FM {Angle}");
            var response = await service.ReadAsync();
            if (response == "OK")
            {
                Model.IsRunning = response == "OK";
                Model.CurrentPos = 0;
                _oldStartAngle = _startAngle;

                service.BeginListening((s, a) => OnDataReseived(a.Text));
                return true;
            }

            return false;
        }

        public void OnDataReseived(string text)
        {
            Console.WriteLine($"Received text: '{text}'");
            //            else if (text == "END") //TODO temporary
            if (text.Contains("END")) //TODO temporary
            {
                // finished
                var service = DependencyService.Resolve<IBluetoothService>();
Console.WriteLine($"EndListening");
                service.EndListening();
                Model.CurrentPos = 0;
                _startAngle = _endAngle = 0;
                return;
            }
            if (text.StartsWith("POS "))
            {
                Model.CurrentPos = int.TryParse(text.Substring(4), out int i) ? i : 0;

//                if (Angle > 0)
                    _startAngle = _oldStartAngle + Model.CurrentPos;
//                else
//                    _startAngle += _oldEndAngle + Model.CurrentPos;
                Console.WriteLine($"_startAngle: {_startAngle}");

CanvasView.InvalidateSurface(); // TODO
            }
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
    }
}
