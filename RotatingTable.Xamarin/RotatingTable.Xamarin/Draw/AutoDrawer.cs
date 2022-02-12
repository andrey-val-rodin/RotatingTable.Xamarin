using RotatingTable.Xamarin.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Forms;

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
            DrawSector(90, 360 * Model.CurrentStep / Model.Steps);
            DrawBorder();
            DrawText(0, 40, Model.CurrentStep.ToString(), 100, SKTextAlign.Center);
        }
    }
}
