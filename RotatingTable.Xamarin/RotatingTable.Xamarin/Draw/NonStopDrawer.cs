using RotatingTable.Xamarin.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Forms;

namespace RotatingTable.Xamarin.Draw
{
    public class NonStopDrawer : ChangablePWMDriver
    {
        public NonStopDrawer(SKCanvasView canvasView, MainModel model) : base(canvasView, model)
        {
        }

        public override void Draw(SKPaintSurfaceEventArgs args)
        {
            base.Draw(args);

            DrawCircle();
            DrawMarker();
            DrawArrow();
            DrawBorder();
            DrawText(0, 40, Model.CurrentStep.ToString(), 100, SKTextAlign.Center);
        }
    }
}
