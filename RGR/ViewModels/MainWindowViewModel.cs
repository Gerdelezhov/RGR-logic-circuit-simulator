using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using RGR.Models;
using ReactiveUI;
using System.Collections.Generic;

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
        Canvas canv = new();
        readonly main map = new();
        public string Logg { get => log; set => this.RaiseAndSetIfChanged(ref log, value); }

        public MainWindowViewModel() {
            Log.Mwvm = this;
        }

        public void AddWindow(Window mw) {
            var canv = mw.Find<Canvas>("Canvas");
            if (canv == null) return;
            this.canv = canv;

            var panel = (Panel?) canv.Parent;
            if (panel == null) return;

            panel.PointerPressed += (object? sender, PointerPressedEventArgs e) => {
                if (e.Source != null && e.Source is Control @control) map.Press(@control, e.GetCurrentPoint(canv).Position);
            };
            panel.PointerMoved += (object? sender, PointerEventArgs e) => {
                if (e.Source != null && e.Source is Control @control) map.Move(@control, e.GetCurrentPoint(canv).Position);
            };
            panel.PointerReleased += (object? sender, PointerReleasedEventArgs e) => {
                if (e.Source != null && e.Source is Control @control) {
                    var pos = e.GetCurrentPoint(canv).Position;
                    map.Release(@control, pos);
                }
            };
            panel.PointerWheelChanged += (object? sender, PointerWheelEventArgs e) => {
                if (e.Source != null && e.Source is Control @control) map.WheelMove(@control, e.Delta.Y);
            };
        }
    }
}