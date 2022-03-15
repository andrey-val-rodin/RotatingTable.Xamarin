using RotatingTable.Xamarin.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;

namespace RotatingTable.Xamarin.Draw
{
    public class ChangablePWMDriver : BaseDrawer
    {
        public ChangablePWMDriver(SKCanvasView canvasView, MainViewModel model) : base(canvasView, model)
        {
        }

        protected void DrawArrow()
        {
            if (Model.IsDecreasingPWM)
            {
                DrawArrow(325, 305, 24);
            }
            if (Model.IsIncreasingPWM)
            {
                DrawArrow(215, 235, 24);
            }
        }

        public override void Clear()
        {
        }
    }
}
