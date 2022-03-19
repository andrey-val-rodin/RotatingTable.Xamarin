using RotatingTable.Xamarin.TouchTracking;
using RotatingTable.Xamarin.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using Xamarin.Forms;

namespace RotatingTable.Xamarin.Draw
{
    public abstract class BaseDrawer
    {
        protected readonly SKPaint _paint = new()
        {
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            StrokeWidth = 1,
            IsAntialias = true
        };

        protected SKCanvasView CanvasView { get; }
        protected MainViewModel Model { get; }
        protected SKCanvas Canvas { get; private set; }
        protected int Radius { get; private set; }
        protected SKPoint Center { get; private set; }
        protected SKRect Rect { get; private set; }

        public BaseDrawer(SKCanvasView canvasView, MainViewModel model)
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

        protected void DrawText(SKPoint pt, string text, int height, SKTextAlign align)
        {
            DrawText(pt.X, pt.Y, text, height, align);
        }

        protected void DrawText(float x, float y, string text, int height, SKTextAlign align)
        {
            _paint.Style = SKPaintStyle.Fill;
            _paint.Shader = null;
            _paint.Color = new SKColor(0, 0, 0);
            _paint.TextSize = height;
            _paint.TextAlign = align;
            Canvas.DrawText(text, x, y, _paint);
        }

        protected void DrawSector(int startAngle, int endAngle)
        {
            var path = new SKPath();
            path.MoveTo(0, 0);
            path.ArcTo(Rect, startAngle, endAngle, false);
            path.LineTo(0, 0);

            _paint.Style = SKPaintStyle.Fill;
            _paint.Shader = null;
            _paint.Color = ((Color)Application.Current.Resources["Highlight"]).ToSKColor();
            Canvas.DrawPath(path, _paint);
        }

        protected void DrawArrow(int startAngle, int endAngle, int indent)
        {
            var angle = endAngle - startAngle;

            if (Math.Abs(angle) < 8)
                return;

            var path = new SKPath();
            var rect = Rect;
            rect.Inflate(indent, indent);
            path.ArcTo(rect, startAngle, angle, true);

            // Draw arc
            _paint.Style = SKPaintStyle.Stroke;
            _paint.Shader = null;
            _paint.Color = ((Color)Application.Current.Resources["Arrow"]).ToSKColor();
            Canvas.DrawPath(path, _paint);

            // Draw arrow
            _paint.Style = SKPaintStyle.Fill;
            path = new SKPath();
            if (angle > 0)
            {
                path.MoveTo(GetCirclePt(endAngle, Radius + indent));
                path.LineTo(GetCirclePt(endAngle - 4, Radius + indent + 5));
                path.LineTo(GetCirclePt(endAngle - 4, Radius + indent - 5));
                path.Close();
                Canvas.DrawPath(path, _paint);
            }
            else
            {
                path.MoveTo(GetCirclePt(endAngle, Radius + indent));
                path.LineTo(GetCirclePt(endAngle + 4, Radius + indent + 5));
                path.LineTo(GetCirclePt(endAngle + 4, Radius + indent - 5));
                path.Close();
                Canvas.DrawPath(path, _paint);
            }
        }

        protected void DrawMarker(int pos)
        {
            var angle = pos % 360 + 89.8;
            _paint.Style = SKPaintStyle.Fill;
            _paint.Shader = null;
            _paint.Color = ((Color)Application.Current.Resources["Marker"]).ToSKColor();
            Canvas.DrawCircle(GetCirclePt(angle, Radius - 24), 10, _paint);
        }

        protected SKPoint GetCirclePt(double angleDegrees, int raduis)
        {
            return new SKPoint
            {
                X = (float)(Math.Cos(ToRadians(angleDegrees)) * raduis),
                Y = (float)(Math.Sin(ToRadians(angleDegrees)) * raduis)
            };
        }

        public virtual void OnTouchEffectAction(object sender, TouchActionEventArgs args)
        {
        }

        public abstract void Clear();

        public static double ToDegrees(double radians) => radians * 180.0 / Math.PI;
        public static double ToRadians(double degrees) => Math.PI * degrees / 180.0;
    }
}
