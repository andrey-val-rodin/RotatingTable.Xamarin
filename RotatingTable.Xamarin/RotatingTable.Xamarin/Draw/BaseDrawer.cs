using RotatingTable.Xamarin.TouchTracking;
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

        protected SKCanvasView CanvasView { get; }
        protected MainModel Model { get; }
        protected SKCanvas Canvas { get; private set; }
        protected int Radius { get; private set; }
        protected SKPoint Center { get; private set; }
        protected SKRect Rect { get; private set; }

        public BaseDrawer(SKCanvasView canvasView, MainModel model)
        {
            CanvasView = canvasView;
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
            Center = new SKPoint(args.Info.Rect.MidX, args.Info.Rect.MidY);
            Canvas.SetMatrix(SKMatrix.CreateTranslation(Center.X, Center.Y));
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

        protected void DrawText()
        {
            _paint.Style = SKPaintStyle.Fill;
            _paint.Shader = null;
            _paint.Color = new SKColor(0, 0, 0);
            var text = Model.CurrentStep.ToString();
            var width = _paint.MeasureText(text);
            Canvas.DrawText(text, -width / 2, _paint.TextSize / 2, _paint);
        }

        protected SKPoint GetCirclePt(int angleDegrees, int raduis)
        {
            return new SKPoint
            {
                X = (float)Math.Cos(ToRadians(angleDegrees)) * raduis,
                Y = (float)Math.Sin(ToRadians(angleDegrees)) * raduis
            };
        }

        public virtual void OnTouchEffectAction(object sender, TouchActionEventArgs args)
        {
        }

        public static double ToDegrees(double radians) => radians * 180 / Math.PI;
        public static double ToRadians(double degrees) => Math.PI * degrees / 180;
    }
}
