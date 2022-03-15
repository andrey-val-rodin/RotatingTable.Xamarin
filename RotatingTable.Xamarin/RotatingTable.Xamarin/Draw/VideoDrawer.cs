using RotatingTable.Xamarin.ViewModels;
using SkiaSharp.Views.Forms;

namespace RotatingTable.Xamarin.Draw
{
    public class VideoDrawer : ChangablePWMDriver
    {
        public VideoDrawer(SKCanvasView canvasView, MainModel model) : base(canvasView, model)
        {
        }

        public override void Draw(SKPaintSurfaceEventArgs args)
        {
            base.Draw(args);

            DrawCircle();
            DrawMarker(Model.CurrentPos);
            DrawArrow();
            DrawBorder();
        }
    }
}
