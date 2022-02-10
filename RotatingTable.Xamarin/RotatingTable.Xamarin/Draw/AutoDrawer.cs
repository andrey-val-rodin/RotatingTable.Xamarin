using RotatingTable.Xamarin.ViewModels;
using SkiaSharp.Views.Forms;

namespace RotatingTable.Xamarin.Draw
{
    public class AutoDrawer : BaseDrawer
    {
        public AutoDrawer(MainModel model) : base(model)
        {
        }

        public override void Draw(SKPaintSurfaceEventArgs args)
        {
            base.Draw(args);

            DrawCircle();
            DrawSelectedSector();
            DrawBorder();
            DrawText();
        }
    }
}
