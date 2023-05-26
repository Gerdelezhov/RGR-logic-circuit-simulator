using Avalonia.Controls;
using Avalonia;
using System;

namespace RGR.Models {
    public static class auxiliary {

        public static int Normalize(this int num, int min, int max) {
            if (num < min) return min;
            if (num > max) return max;
            return num;
        }
        public static double Normalize(this double num, double min, double max) {
            if (num < min) return min;
            if (num > max) return max;
            return num;
        }

        public static double Hypot(this Point delta) {
            return Math.Sqrt(Math.Pow(delta.X, 2) + Math.Pow(delta.Y, 2));
        }
        public static double Hypot(this Point A, Point B) {
            Point delta = A - B;
            return Math.Sqrt(Math.Pow(delta.X, 2) + Math.Pow(delta.Y, 2));
        }

        public static double? ToDouble(this object num) {
            return num switch {
                int @int => @int,
                long @long => @long,
                double @double => @double,
                _ => null,
            };
        }

        public static int Min(this int A, int B) => A < B ? A : B;
        public static int Max(this int A, int B) => A > B ? A : B;
        public static double Min(this double A, double B) => A < B ? A : B;
        public static double Max(this double A, double B) => A > B ? A : B;

        public static void Remove(this Control item) {
            var p = (Panel?) item.Parent;
            p?.Children.Remove(item);
        }

        public static Point Center(this Visual item, Visual? parent) {
            var tb = item.TransformedBounds;
            if (tb == null) return new(); // Обычно так не бывает
            var bounds = tb.Value.Bounds.TransformToAABB(tb.Value.Transform);
            var res = bounds.Center;
            if (parent == null) return res; // parent в качестве точки отсчёта, например холст

            var tb2 = parent.TransformedBounds;
            if (tb2 == null) return res; // Обычно так не бывает
            var bounds2 = tb2.Value.Bounds.TransformToAABB(tb2.Value.Transform);
            return res - bounds2.TopLeft;
        }
    }
}
