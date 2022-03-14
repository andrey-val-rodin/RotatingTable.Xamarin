using RotatingTable.Xamarin.Models;
using RotatingTable.Xamarin.Services;
using RotatingTable.Xamarin.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace RotatingTable.Xamarin.Draw
{
    public abstract class RotateDrawer : BaseDrawer
    {
        protected bool _isBusy = false;
        protected bool _isDragging = false;
        protected int _offset = 0;

        public RotateDrawer(SKCanvasView canvasView, MainModel model) : base(canvasView, model)
        {
        }

        protected abstract int StartAngle { get; set; }
        protected abstract int EndAngle { get; set; }
        protected int Angle
        {
            get => EndAngle - StartAngle;
        }

        public override void Draw(SKPaintSurfaceEventArgs args)
        {
            base.Draw(args);

            DrawCircle();
            DrawSelectedSector();
            DrawArrow(StartAngle, EndAngle, -24);
            if (_isDragging)
                DrawText(-Radius, -Radius + 50, $"{Angle}°", 40, SKTextAlign.Left);
            DrawBorder();
        }

        private void DrawSelectedSector()
        {
            if (Angle == 0)
                return;

            DrawSector(StartAngle, Angle);
        }

        protected async Task<bool> StartMovementAsync()
        {
            if (Angle == 0)
                return false;

            var service = DependencyService.Resolve<IBluetoothService>();
            if (!await service.RotateAsync(Angle, (s, a) => OnDataReseived(a.Text)))
                return false;

            Model.IsRunning = true;
            Model.CurrentPos = 0;
            _isBusy = true;
            return true;
        }

        protected void OnDataReseived(string text)
        {
            Debug.WriteLine($"Received text: '{text}'");
            if (text == Commands.End)
            {
                // finished
                Model.CurrentPos = 0;
                CanvasView.InvalidateSurface();
                Clear();
                return;
            }
            if (text.StartsWith(Commands.Position))
            {
                Model.CurrentPos = int.TryParse(text.Substring(4), out int i) ? i : 0;
                _offset = Model.CurrentPos;
            }
        }

        protected SKPoint Transform(Point pt)
        {
            var point = new SKPoint((float)(CanvasView.CanvasSize.Width * pt.X / CanvasView.Width),
                               (float)(CanvasView.CanvasSize.Height * pt.Y / CanvasView.Height));
            point.Offset(-Center.X, -Center.Y);
            return point;
        }

        public override void Clear()
        {
            _isBusy = false;
            _isDragging = false;
            _offset = 0;
        }

        protected bool IsInsideCircle(SKPoint pt)
        {
            var hypotenuse = Math.Sqrt(pt.X * pt.X + pt.Y * pt.Y);
            return hypotenuse < Radius;
        }

        protected double ToAngle(SKPoint pt)
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
