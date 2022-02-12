using RotatingTable.Xamarin.ViewModels;
using SkiaSharp.Views.Forms;

namespace RotatingTable.Xamarin.Draw
{
    public class Rotate90Drawer : BaseDrawer
    {
        public Rotate90Drawer(SKCanvasView canvasView, MainModel model) : base(canvasView, model)
        {
        }

        public override void Draw(SKPaintSurfaceEventArgs args)
        {
            base.Draw(args);

            DrawCircle();
            //DrawSelectedSector(); TODO
            DrawBorder();
        }
    }
}
