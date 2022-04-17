using RotatingTable.Xamarin.ViewModels;
using SkiaSharp.Views.Forms;

namespace RotatingTable.Xamarin.Draw
{
    public class ChangablePWMDrawer : BaseDrawer
    {
        public ChangablePWMDrawer(SKCanvasView canvasView, MainViewModel model) : base(canvasView, model)
        {
        }

        protected void DrawArrow()
        {
            switch (Model.ChangingPWM)
            {
                case ChangePWM.Increase:
                    DrawArrow(215, 235, 24);
                    break;
                case ChangePWM.Decrease:
                    DrawArrow(325, 305, 24);
                    break;
            }
        }

        public override void Clear()
        {
        }
    }
}
