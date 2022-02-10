using RotatingTable.Xamarin.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using Xamarin.Forms;

namespace RotatingTable.Xamarin.Draw
{
    public class BaseDrawer
    {
        protected readonly SKPaint _paint = new()
        {
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            StrokeWidth = 1,
            TextSize = 100,
            IsAntialias = true
        };

        protected MainModel Model { get; }
        protected SKCanvas Canvas { get; private set; }
        protected SKRect Rect { get; private set; }
        protected int Radius { get; private set; }

        public BaseDrawer(MainModel model)
        {
            Model = model;
        }

        public virtual void Draw(SKPaintSurfaceEventArgs args)
        {
            DefineDrawArea(args);
            Canvas.Clear();
        }

        private void DefineDrawArea(SKPaintSurfaceEventArgs args)
        {
            Canvas = args.Surface.Canvas;

            Radius = Math.Min(args.Info.Rect.Width, args.Info.Rect.Height) / 2;
            var center = new SKPoint(args.Info.Rect.MidX, args.Info.Rect.MidY);
            Canvas.SetMatrix(SKMatrix.CreateTranslation(center.X, center.Y));
            Rect = new(-Radius, -Radius, Radius, Radius);
        }

        protected void DrawCircle()
        {
            _paint.Style = SKPaintStyle.Fill;
            var colors = new SKColor[] {
                ((Color)Application.Current.Resources["SurfaceStart"]).ToSKColor(),
                ((Color)Application.Current.Resources["SurfaceEnd"]).ToSKColor()
            };
            var shader = SKShader.CreateRadialGradient(
                new SKPoint(0, 0),
                Radius,
                colors,
                SKShaderTileMode.Clamp);
            _paint.Shader = shader;
            Canvas.DrawOval(Rect, _paint);
        }

        protected void DrawBorder()
        {
            _paint.Style = SKPaintStyle.Stroke;
            _paint.Shader = null;
            _paint.Color = ((Color)Application.Current.Resources["Border"]).ToSKColor();
            Canvas.DrawOval(Rect, _paint);
        }

        protected void DrawSelectedSector()
        {
            var path = new SKPath();
            path.MoveTo(0, 0);
            var angle = 360 * Model.CurrentStep / Model.Steps;
            path.ArcTo(Rect, 90, angle, false);
            path.LineTo(0, 0);

            _paint.Style = SKPaintStyle.Fill;
            _paint.Shader = null;
            _paint.Color = ((Color)Application.Current.Resources["Highlight"]).ToSKColor();
            Canvas.DrawPath(path, _paint);
        }

        protected void DrawText()
        {
            _paint.Style = SKPaintStyle.Fill;
            _paint.Shader = null;
            _paint.Color = new SKColor(0, 0, 0);
            var text = Model.CurrentStep.ToString();
            var width = _paint.MeasureText(text);
            Canvas.DrawText(text, -width / 2, _paint.TextSize / 2, _paint);
        }
    }
}
