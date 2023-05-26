using Avalonia.Controls;

namespace RGR.Models {
    public static class auxiliary
    {
        public static double Min(this double A, double B) => A < B ? A : B;
        public static double Max(this double A, double B) => A > B ? A : B;

        public static void Remove(this Control item) {
            var p = (Panel?) item.Parent;
            p?.Children.Remove(item);
        }
    }
}
