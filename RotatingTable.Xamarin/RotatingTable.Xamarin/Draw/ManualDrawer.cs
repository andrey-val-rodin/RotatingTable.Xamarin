using RotatingTable.Xamarin.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Forms;

namespace RotatingTable.Xamarin.Draw
{
    public class ManualDrawer : BaseDrawer
    {
        public ManualDrawer(SKCanvasView canvasView, MainViewModel model) : base(canvasView, model)
        {
        }

        public override void Draw(SKPaintSurfaceEventArgs args)
        {
            base.Draw(args);

            DrawCircle();
            if (Model.CurrentStep > 0)
            {
                var angle = 360 / Model.Steps;
                var anchor = angle * (Model.CurrentStep - 1) + 90;
                DrawSector(anchor + Model.CurrentPos, angle - Model.CurrentPos);

                if (Model.CurrentPos == 0)
                    DrawMarker(anchor - 90);

                DrawText(0, 40, Model.CurrentStep.ToString(), 100, SKTextAlign.Center);
            }

            DrawBorder();
        }

        public override void Clear()
        {
        }
    }
}
