using Foundation;

namespace RotatingTable.Xamarin.iOS
{
    [Preserve(AllMembers = true)]
    public class LinkerPleaseLink
    {
        public void Include(MvvmCross.Plugins.BLE.iOS.Plugin plugin)
        {
            plugin.Load();
        }
    }
}