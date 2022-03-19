using RotatingTable.Xamarin.ViewModels;
using SkiaSharp.Views.Forms;

namespace RotatingTable.Xamarin.Draw
{
    public class VideoDrawer : ChangablePWMDrawer
    {
        public VideoDrawer(SKCanvasView canvasView, MainViewModel model) : base(canvasView, model)
        {
        }

        public override void Draw(SKPaintSurfaceEventArgs args)
        {
            base.Draw(args);

            DrawCircle();

            if (Model.CurrentPos != 0)
            {
                DrawMarker(Model.CurrentPos);
                DrawArrow();
            }

            DrawBorder();
        }
    }
}
