using RotatingTable.Xamarin.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;

namespace RotatingTable.Xamarin.Draw
{
    public class AutoDrawer : BaseDrawer
    {
        public AutoDrawer(SKCanvasView canvasView, MainModel model) : base(canvasView, model)
        {
        }

        public override void Draw(SKPaintSurfaceEventArgs args)
        {
            base.Draw(args);

            DrawCircle();
            DrawSector();
            DrawBorder();
            DrawText();
        }

        private void DrawSector()
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
    }
}
