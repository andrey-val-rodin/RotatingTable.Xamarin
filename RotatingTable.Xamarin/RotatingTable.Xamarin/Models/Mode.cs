namespace RotatingTable.Xamarin.Models
{
    public enum Mode : int
    {
        First = 0,
        Auto = First,
        Manual = 1,
        Nonstop = 2,
        Video = 3,
        Rotate90 = 4,
        FreeMovement = 5,
        Last = FreeMovement
    };
}
