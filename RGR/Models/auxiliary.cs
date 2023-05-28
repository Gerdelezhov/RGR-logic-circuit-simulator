using System.Text;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia;
using Avalonia.Media;
using System.Text.Json;
using RGR.ViewModels;
using System.Collections;
using System.Diagnostics;
using System;
using Avalonia.Controls.Shapes;

namespace RGR.Models {
    public static class auxiliary {
        // JSON

        public static string JsonEscape(string str) {
            StringBuilder sb = new();
            foreach (char i in str) {
                sb.Append(i switch {
                    '"' => "\\\"",
                    '\\' => "\\\\",
                    '$' => "{$",
                    _ => i
                });
            }
            return sb.ToString();
        }
        public static string Obj2json(object? obj) {
            switch (obj) {
            case null: return "null";
            case string @str: return '"' + JsonEscape(str) + '"';
            case bool @bool: return @bool ? "true" : "false";
            case short @short: return @short.ToString();
            case int @int: return @int.ToString();
            case long @long: return @long.ToString();
            case float @float: return @float.ToString().Replace(',', '.');
            case double @double: return @double.ToString().Replace(',', '.');

            case Point @point: return "\"$p$" + (int) @point.X + "," + (int) @point.Y + '"';
            case Size @size: return "\"$s$" + (int) @size.Width + "," + (int) @size.Height + '"';
            case Points @points: return "\"$P$" + string.Join("|", @points.Select(p => (int) p.X + "," + (int) p.Y)) + '"';
            case SolidColorBrush @color: return "\"$C$" + @color.Color + '"';
            case Thickness @thickness: return "\"$T$" + @thickness.Left + "," + @thickness.Top + "," + @thickness.Right + "," + @thickness.Bottom + '"';

            case Dictionary<string, object?> @dict: {
                StringBuilder sb = new();
                sb.Append('{');
                foreach (var entry in @dict) {
                    if (sb.Length > 1) sb.Append(", ");
                    sb.Append(Obj2json(entry.Key));
                    sb.Append(": ");
                    sb.Append(Obj2json(entry.Value));
                }
                sb.Append('}');
                return sb.ToString();
            }
            case IEnumerable @list: {
                StringBuilder sb = new();
                sb.Append('[');
                foreach (object? item in @list) {
                    if (sb.Length > 1) sb.Append(", ");
                    sb.Append(Obj2json(item));
                }
                sb.Append(']');
                return sb.ToString();
            }
            default: return "(" + obj.GetType() + " ???)";
            }
        }

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
            if (tb == null) return new();
            var bounds = tb.Value.Bounds.TransformToAABB(tb.Value.Transform);
            var res = bounds.Center;
            if (parent == null) return res; // parent в качестве точки отсчёта, например холст

            var tb2 = parent.TransformedBounds;
            if (tb2 == null) return res;
            var bounds2 = tb2.Value.Bounds.TransformToAABB(tb2.Value.Transform);
            return res - bounds2.TopLeft;
        }
    }
}
