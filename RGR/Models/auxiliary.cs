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
using System;

namespace RGR.Models {
    public static class auxiliary {
        /*
         * JSON
         */

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

        private static object JsonHandler(string str) {
            if (str.Length < 3 || str[0] != '$' || str[2] != '$') return str.Replace("{$", "$");
            string data = str[3..];
            string[] thick = str[1] == 'T' ? data.Split(',') : System.Array.Empty<string>();
            return str[1] switch {
                'p' => Point.Parse(data),
                's' => Size.Parse(data),
                'C' => new SolidColorBrush(Color.Parse(data)),
                'T' => new Thickness(double.Parse(thick[0]), double.Parse(thick[1]), double.Parse(thick[2]), double.Parse(thick[3])),
                _ => str,
            };
        }
        private static object? JsonHandler(object? obj) {
            if (obj == null) return null;

            if (obj is List<object?> @list) return @list.Select(JsonHandler).ToList();
            if (obj is Dictionary<string, object?> @dict) {
                return new Dictionary<string, object?>(@dict.Select(pair => new KeyValuePair<string, object?>(pair.Key, JsonHandler(pair.Value))));
            }
            if (obj is JsonElement @item) {
                switch (@item.ValueKind) {
                case JsonValueKind.Undefined: return null;
                case JsonValueKind.Object:
                    Dictionary<string, object?> res = new();
                    foreach (var el in @item.EnumerateObject()) res[el.Name] = JsonHandler(el.Value);
                    return res;
                case JsonValueKind.Array:
                    List<object?> res2 = @item.EnumerateArray().Select(item => JsonHandler((object?) item)).ToList();
                    return res2;
                case JsonValueKind.String:
                    var s = JsonHandler(@item.GetString() ?? "");
                    return s;
                case JsonValueKind.Number:
                    if (@item.ToString().Contains('.')) return @item.GetDouble();
                    long a = @item.GetInt64();
                    int b = @item.GetInt32();
                    if (a != b) return a;
                    return b;
                case JsonValueKind.True: return true;
                case JsonValueKind.False: return false;
                case JsonValueKind.Null: return null;
                }
            }
            Log.Write("JT: " + obj.GetType());

            return obj;
        }
        public static object? Json2obj(string json) {
            json = json.Trim();
            if (json.Length == 0) return null;

            object? data;
            if (json[0] == '[') data = JsonSerializer.Deserialize<List<object?>>(json);
            else if (json[0] == '{') data = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
            else return null;

            return JsonHandler(data);
        }

        /*
         * XML
         */

        public static string XMLEscape(string str) {
            StringBuilder sb = new();
            foreach (char i in str) {
                sb.Append(i switch {
                    '"' => "&quot;",
                    '\'' => "&apos;",
                    '>' => "&gt;",
                    '<' => "&lt;",
                    '&' => "&amp;",
                    _ => i
                });
            }
            return sb.ToString();
        }

        private static bool IsComposite(object? obj) {
            if (obj == null) return false;
            if (obj is List<object?> || obj is Dictionary<string, object?> || obj is not JsonElement @item) return true;
            var T = @item.ValueKind;
            return T == JsonValueKind.Object || T == JsonValueKind.Array;
        }
        private static string Dict2XML(Dictionary<string, object?> dict, string level) {
            StringBuilder attrs = new();
            StringBuilder items = new();
            foreach (var entry in dict)
                if (IsComposite(entry.Value))
                    items.Append(level + "\t<" + entry.Key + ">" + ToXMLHandler(entry.Value, level + "\t\t") + level + "\t</" + entry.Key + ">");
                else attrs.Append(" " + entry.Key + "=\"" + ToXMLHandler(entry.Value, "{err}") + "\"");

            if (items.Length == 0) return level + "<Dict" + attrs.ToString() + "/>";
            return level + "<Dict" + attrs.ToString() + ">" + items.ToString() + level + "</Dict>";
        }
        private static string List2XML(List<object?> list, string level) {
            StringBuilder attrs = new();
            StringBuilder items = new();
            int num = 0;
            foreach (var entry in list) {
                if (IsComposite(entry)) items.Append(ToXMLHandler(entry, level + "\t"));
                else attrs.Append($" _{num}='" + ToXMLHandler(entry, "{err}") + "'");
                num++;
            }

            if (items.Length == 0) return level + "<List" + attrs.ToString() + "/>";
            return level + "<List" + attrs.ToString() + ">" + items.ToString() + level + "</List>";
        }

        private static string ToXMLHandler(object? obj, string level) {
            if (obj == null) return "null";

            if (obj is List<object?> @list) return List2XML(@list, level);
            if (obj is Dictionary<string, object?> @dict) return Dict2XML(@dict, level);
            if (obj is JsonElement @item) {
                switch (@item.ValueKind) {
                case JsonValueKind.Undefined: return "undefined";
                case JsonValueKind.Object:
                    return Dict2XML(new Dictionary<string, object?>(@item.EnumerateObject().Select(pair => new KeyValuePair<string, object?>(pair.Name, pair.Value))), level);
                case JsonValueKind.Array:
                    return List2XML(@item.EnumerateArray().Select(item => (object?) item).ToList(), level);
                case JsonValueKind.String:
                    var s = XMLEscape(@item.GetString() ?? "null");
                    return s;
                case JsonValueKind.Number: return "$" + @item.ToString(); // escape NUM
                case JsonValueKind.True: return "_BOOL_yeah";
                case JsonValueKind.False: return "_BOOL_nop";
                case JsonValueKind.Null: return "null";
                }
            }
            Log.Write("XT: " + obj.GetType());

            return "<UnknowType>" + obj.GetType() + "</UnknowType>";
        }
        public static string? Json2xml(string json) {
            json = json.Trim();
            if (json.Length == 0) return null;

            object? data;
            if (json[0] == '[') data = JsonSerializer.Deserialize<List<object?>>(json);
            else if (json[0] == '{') data = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
            else return null;

            return "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + ToXMLHandler(data, "\n");
        }

        private static string ToJSONHandler(string str) {
            if (str.Length > 1 && str[0] == '$' && str[1] <= '9' && str[1] >= '0') return str[1..]; // unescape NUM
            return str switch {
                "null" => "null",
                "undefined" => "undefined",
                "_BOOL_yeah" => "true",
                "_BOOL_nop" => "false",
                _ => '"' + str.Replace("\\", "\\\\") + '"',
            };
        }
        private static string ToJSONHandler(XElement xml) {
            var name = xml.Name.LocalName;
            StringBuilder sb = new();
            if (name == "Dict") {
                sb.Append('{');
                foreach (var attr in xml.Attributes()) {
                    if (sb.Length > 1) sb.Append(", ");
                    sb.Append(ToJSONHandler(attr.Name.LocalName));
                    sb.Append(": ");
                    sb.Append(ToJSONHandler(attr.Value));
                }
                foreach (var el in xml.Elements()) {
                    if (sb.Length > 1) sb.Append(", ");
                    sb.Append(ToJSONHandler(el.Name.LocalName));
                    sb.Append(": ");
                    sb.Append(ToJSONHandler(el.Elements().ToArray()[0]));
                }
                sb.Append('}');
            } else if (name == "List") {
                var attrs = xml.Attributes().ToArray();
                var els = xml.Elements().ToArray();
                int count = attrs.Length + els.Length;
                var res = new string[count];
                var used = new bool[count];
                int num;
                foreach (var attr in attrs) {
                    num = int.Parse(attr.Name.LocalName[1..]);
                    res[num] = ToJSONHandler(attr.Value);
                    used[num] = true;
                }
                num = 0;
                foreach (var el in els) {
                    while (used[num]) num++;
                    res[num++] = ToJSONHandler(el);
                }
                sb.Append('[');
                foreach (var item in res) {
                    if (sb.Length > 1) sb.Append(", ");
                    sb.Append(item);
                }
                sb.Append(']');
            } else sb.Append("Type??" + name);
            return sb.ToString();
        }
        public static string Xml2json(string xml) => ToJSONHandler(XElement.Parse(xml));


        /*
         * Остальное
         */

        public static string? Obj2xml(object? obj) => Json2xml(Obj2json(obj));
        public static object? Xml2obj(string xml) => Json2obj(Xml2json(xml));

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
            if (parent == null) return res;

            var tb2 = parent.TransformedBounds;
            if (tb2 == null) return res;
            var bounds2 = tb2.Value.Bounds.TransformToAABB(tb2.Value.Transform);
            return res - bounds2.TopLeft;
        }

        public static DateTime UnixTimeStampToDateTime(this long unixTimeStamp) {
            DateTime dateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }
        public static string UnixTimeStampToString(this long unixTimeStamp) {
            return UnixTimeStampToDateTime(unixTimeStamp).ToString("yyyy/MM/dd H:mm:ss");
        }
    }
}
