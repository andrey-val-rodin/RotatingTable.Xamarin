using RotatingTable.Xamarin.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;

namespace RotatingTable.Xamarin.Draw
{
    public class ChangablePWMDriver : BaseDrawer
    {
        public ChangablePWMDriver(SKCanvasView canvasView, MainModel model) : base(canvasView, model)
        {
        }

        protected void DrawMarker()
        {
            var angle = Model.CurrentPos % 360 + 90;
            _paint.Style = SKPaintStyle.Fill;
            _paint.Shader = null;
            _paint.Color = Color.Blue.ToSKColor();
            Canvas.DrawCircle(GetCirclePt(angle, Radius - 24), 10, _paint);
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
