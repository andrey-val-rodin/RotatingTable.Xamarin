using RotatingTable.Xamarin.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Forms;

namespace RotatingTable.Xamarin.Draw
{
    public class NonStopDrawer : ChangablePWMDriver
    {
        public NonStopDrawer(SKCanvasView canvasView, MainViewModel model) : base(canvasView, model)
        {
        }

        public override void Draw(SKPaintSurfaceEventArgs args)
        {
            base.Draw(args);
            DrawCircle();

            if (Model.CurrentStep > 0)
            {
                DrawMarker(Model.CurrentPos);
                DrawArrow();
                DrawText(0, 40, Model.CurrentStep.ToString(), 100, SKTextAlign.Center);
            }

            DrawBorder();
        }
    }
}
