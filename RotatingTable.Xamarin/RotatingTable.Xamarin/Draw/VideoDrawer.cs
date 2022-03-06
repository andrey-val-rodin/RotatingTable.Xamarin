using RotatingTable.Xamarin.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;

namespace RotatingTable.Xamarin.Draw
{
    public class VideoDrawer : BaseDrawer
    {
        public VideoDrawer(SKCanvasView canvasView, MainModel model) : base(canvasView, model)
        {
        }

        public override void Draw(SKPaintSurfaceEventArgs args)
        {
            base.Draw(args);

            DrawCircle();
            DrawMarker();
            DrawBorder();
        }

        private void DrawMarker()
        {
            var angle = Model.CurrentPos % 360 + 90;
            _paint.Style = SKPaintStyle.Fill;
            _paint.Shader = null;
            _paint.Color = Color.Blue.ToSKColor();
            Canvas.DrawCircle(GetCirclePt(angle, Radius - 24), 10, _paint);
        }

        public override void Clear()
        {
        }
    }
}
