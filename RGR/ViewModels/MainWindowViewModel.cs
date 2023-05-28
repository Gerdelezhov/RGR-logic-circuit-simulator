using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using RGR.Models;
using RGR.Views.Shapes;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RGR.ViewModels {
    public class Log {
        static readonly List<string> logs = new();
        public static MainWindowViewModel? Mwvm { private get; set; }
        public static void Write(string message, bool without_update = false) {
            if (!without_update) {
                foreach (var mess in message.Split('\n')) logs.Add(mess);
                while (logs.Count > 50) logs.RemoveAt(0);

                if (Mwvm != null) Mwvm.Logg = string.Join('\n', logs);
            }
        }
    }

    public class MainWindowViewModel: ViewModelBase {
        private string log = "";
        readonly main map = new();
        public string Logg { get => log; set => this.RaiseAndSetIfChanged(ref log, value); }

        public MainWindowViewModel() {
            Log.Mwvm = this;
        }

        public void AddWindow(Window mw) {
            var canv = mw.Find<Canvas>("Canvas");
            if (canv == null) return;

            canv.Children.Add(map.Marker);

            var panel = (Panel?) canv.Parent;
            if (panel == null) return;

            panel.PointerPressed += (object? sender, PointerPressedEventArgs e) => {
                if (e.Source != null && e.Source is Control @control) map.Press(@control, e.GetCurrentPoint(canv).Position);
            };
                        if (canv == null) return;
                if (e.Source != null && e.Source is Control @control) map.Move(@control, e.GetCurrentPoint(canv).Position);
            };
            panel.PointerReleased += (object? sender, PointerReleasedEventArgs e) => {
                if (e.Source != null && e.Source is Control @control) {
                    int mode = map.Release(@control, e.GetCurrentPoint(canv).Position);
                    bool tap = map.tapped;
                    if (tap && mode == 1) {
                        var pos = map.tap_pos;
                        if (canv == null) return;

                        var newy = map.GenSelectedItem();
                        var size = newy.GetSize() / 2;
                        newy.Move(pos - new Point(size.Width, size.Height));
                        canv.Children.Add(newy.GetSelf());
                        map.AddItem(newy);
                    }

                    if (map.new_join != null) {
                        canv.Children.Add(map.new_join);
                        map.new_join = null;
                    }
                }
            };
            panel.PointerWheelChanged += (object? sender, PointerWheelEventArgs e) => {
                if (e.Source != null && e.Source is Control @control) map.WheelMove(@control, e.Delta.Y);
            };
        }

        public IGate[] ItemTypes { get => map.item_types; }
        public int SelectedItem { get => map.SelectedItem; set => map.SelectedItem = value; }

        // Обработка панели со схемами проекта

        Border? cur_border;
        TextBlock? old_b_child;
        readonly ObservableCollection<string> schemes = new() { "scheme_1", "scheme_lol", "scheme_boom" };

        public ObservableCollection<string> Schemes { get => schemes; }



        public void DTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e) {
            Log.Write("DT: " + e.Source);
            var src = (Control?) e.Source;

            if (src is ContentPresenter cp && cp.Child is Border bord) src = bord;
            if (src is Border bord2 && bord2.Child is TextBlock tb2) src = tb2;

            if (src is not TextBlock tb) return;

            var p = tb.Parent;
            if (p == null || p is not Border b) return;

            if (cur_border != null && old_b_child != null) cur_border.Child = old_b_child;
            cur_border = b;
            old_b_child = tb;

            var newy = new TextBox { Text = tb.Text };
            b.Child = newy;
            newy.KeyUp += (object? sender, KeyEventArgs e) => {
                if (e.Key != Key.Return) return;
                tb.Text = newy.Text;
                b.Child = tb;
                cur_border = null; old_b_child = null;

                map.Export();
            };
        }
    }
}