using RotatingTable.Xamarin.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using Xamarin.Forms;

namespace RotatingTable.Xamarin.Draw
{
    public class BaseDrawer
    {
        private readonly SKPaint _paint = new()
        {
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            StrokeWidth = 1,
            TextSize = 100,
            IsAntialias = false
        };

        private MainModel Model { get; }

        public BaseDrawer(MainModel model)
        {
            Model = model;
        }

        public virtual void Draw(SKPaintSurfaceEventArgs args)
        {
            SKCanvas canvas = args.Surface.Canvas;
            canvas.Clear();

            var radius = args.Info.Rect.MidX;
            SKRect rect = args.Info.Rect;
            rect.Offset(-radius, -radius);

            canvas.SetMatrix(SKMatrix.CreateTranslation(radius, radius));

            _paint.Style = SKPaintStyle.Fill;
            var colors = new SKColor[] {
                new SKColor(236, 236, 236),
                new SKColor(200, 200, 200)
            };
            var shader = SKShader.CreateLinearGradient(
                new SKPoint(-args.Info.Rect.MidX, 0),
                new SKPoint(args.Info.Rect.MidX, 0),
                colors,
                null,
                SKShaderTileMode.Clamp);
            _paint.Shader = shader;
            canvas.DrawOval(rect, _paint);

            _paint.Style = SKPaintStyle.Stroke;
            _paint.Color = Color.Black.ToSKColor();
            _paint.Shader = null;
            canvas.DrawOval(rect, _paint);

            var path = new SKPath();
            path.MoveTo(0, 0);
            _paint.Color = Color.Red.ToSKColor();
            var angle = 360 * Model.CurrentStep / Model.Steps;
            path.ArcTo(rect, 90, angle, false);
            path.LineTo(0, 0);

            _paint.Style = SKPaintStyle.Fill;
            _paint.Color = new SKColor(242, 242, 242);
            canvas.DrawPath(path, _paint);

            _paint.Color = new SKColor(0, 0, 0);
            var text = Model.CurrentStep.ToString();
            var width = _paint.MeasureText(text);
            canvas.DrawText(text, -width / 2, _paint.TextSize / 2, _paint);
        }

        private SKPoint Point(int angle, int radius)
        {
            return new SKPoint
            {
                X = (float)-Math.Sin(DegreeToRadian(angle)) * radius,
                Y = (float)-Math.Cos(DegreeToRadian(angle)) * radius
            };
        }

        private double DegreeToRadian(double angle)
        {
            return Math.PI * angle / 180.0;
        }
    }
}
