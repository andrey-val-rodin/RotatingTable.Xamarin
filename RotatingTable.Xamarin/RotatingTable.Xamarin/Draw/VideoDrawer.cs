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
            DrawLine();
            DrawBorder();
        }

        private void DrawLine()
        {
            var angle = Model.CurrentPos % 360 + 90;
            _paint.Style = SKPaintStyle.Stroke;
            _paint.Shader = null;
            _paint.Color = Color.Black.ToSKColor();//((Color)Application.Current.Resources["Border"]).ToSKColor();
            Canvas.DrawLine(new SKPoint(0, 0), GetCirclePt(angle, Radius), _paint);
        }

        public override void Clear()
        {
        }
    }
}
