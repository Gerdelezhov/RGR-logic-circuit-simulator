using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using RGR.Models;
using RGR.ViewModels;
using RGR.Views;
using RGR.Views.Shapes;
using System.Text;
using Button = Avalonia.Controls.Button;

namespace UITests {
    public class AllTestsInOnePlace {
        private readonly LauncherWindow launcherWindow = AvaloniaApp.GetMainWindow();
        private readonly MainWindow mainWindow = LauncherWindowViewModel.GetMW;
        private readonly main map = ViewModelBase.map;

        private readonly Canvas canv;
        private readonly ListBox gates;



        bool first_log = true;
        readonly string path = "../../../TestLog.txt";
#pragma warning disable IDE0051
        private void Log(string? message) {
            message ??= "null";
            if (first_log) {
                File.WriteAllText(path, message + "\n");
                first_log = false;
            }  else File.AppendAllText(path, message + "\n");
        }

        public AllTestsInOnePlace() {
            var buttons = launcherWindow.GetVisualDescendants().OfType<Button>();
            var new_proj = buttons.First(x => (string) x.Content == "Создать новый проект");
            new_proj.Command.Execute(null);

            var vis_arr = mainWindow.GetVisualDescendants();
            canv = vis_arr.OfType<Canvas>().First(x => (string?) x.Tag == "Scene");

            gates = vis_arr.OfType<ListBox>().First(x => x.Name == "Gates");
            map.sim.Stop();
        }



        private IGate? Click(Control target, double x, double y) {
            var pos = new Point(x, y);
            map.Press(target, pos);
            int mode = map.Release(target, pos);
            if (map.tapped && mode == 1) {
                var tpos = map.tap_pos;
                var newy = map.GenSelectedItem();
                newy.Move(tpos);
                map.AddItem(newy);
                return newy;
            }
            return null;
        }
        private void Move(Control a, Control b) {
            map.Move(a, new());
            map.Press(a, new());
            map.Release(b, new(100, 100), false);
        }
        private string Export() {
            map.Export();
            var scheme = map.current_scheme;
            if (scheme == null) return "Scheme not defined";

            scheme.Created = 123;
            scheme.Modified = 456;
            return auxiliary.Obj2json(scheme.Export());
        }
        private void SelectGate(int id) => gates.SelectedIndex = id;
        private void Ticks(int count) {
            while (count-- > 0) map.sim.TopSecretPublicTickMethod();
        }
        private static void SaveProject() {
            var proj = ViewModelBase.TopSecretGetProj() ?? throw new Exception("Проект не найден");
            proj.SetDir("../../..");
            proj.FileName = "tested";
            proj.Save();
        }
        private void NewScheme() {
            Export();
            var button = mainWindow.GetVisualDescendants().OfType<Button>().Last(x => x.Name == "NewScheme");
            button.Command.Execute(null);
            var button2 = mainWindow.GetVisualDescendants().OfType<Button>().Last(x => x.Name == "OpenScheme");
            button2.Command.Execute(null);
        }
        private void ImportScheme(string data) {
            object yeah = auxiliary.Json2obj(data) ?? new Exception("Ошибка в JSON");
            var proj = ViewModelBase.TopSecretGetProj() ?? throw new Exception("Проект не найден");
            Scheme clone = new(proj, yeah);
            var scheme = map.current_scheme ?? throw new Exception("Проект не найден");
            scheme.Update(clone.items, clone.joins, clone.states);
            scheme.Name = clone.Name;
            map.ImportScheme(false);
        }



        private string ComplexSolution(bool lol = false) {
            var sim = map.sim;
            sim.ComparativeTestMode = true;

            var inputs = sim.GetSwitches();
            var outputs = sim.GetLightBulbs();
            int L = inputs.Length;
            int steps = 1 << L;

            StringBuilder sb = new();
            for (int step = 0; step < steps; step++) {
                for (int i = 0; i < L; i++) inputs[i].SetState((step & 1 << i) > 0);
                if (step > 0) sb.Append('|');
                int hits = 0;
                Ticks(1);
                while (++hits < 1024 && sim.SomethingHasChanged) Ticks(1);
                foreach (var output in outputs) sb.Append(output.GetState() ? '1' : '0');
                sb.Append("_t" + hits);
            }
            return sb.ToString();
        }



        [Fact]
        public void GeneralTest() {
            Task.Delay(10).GetAwaiter().GetResult();

            SelectGate(0); // AND-gate
            Task.Delay(1).GetAwaiter().GetResult();

            IGate? gate = Click(canv, 200, 200);
            Assert.NotNull(gate);
            var data = Export();
            Assert.Equal("{\"name\": \"Новая схема\", \"created\": 123, \"modified\": 456, \"items\": [{\"id\": 0, \"pos\": \"$p$200,200\", \"size\": \"$s$71,71\", \"base_size\": 25}], \"joins\": [], \"states\": \"00\"}", data);

            SelectGate(3); // XOR-gate
            Task.Delay(1).GetAwaiter().GetResult();

            IGate? gate2 = Click(canv, 300, 300);
            Assert.NotNull(gate2);

            Move(gate.SecretGetPin(2), gate2.SecretGetPin(0)); // Соединяем gate и gate2

            data = Export();
            Assert.Equal("{\"name\": \"Новая схема\", \"created\": 123, \"modified\": 456, \"items\": [{\"id\": 0, \"pos\": \"$p$200,200\", \"size\": \"$s$71,71\", \"base_size\": 25}, {\"id\": 3, \"pos\": \"$p$300,300\", \"size\": \"$s$71,71\", \"base_size\": 25}], \"joins\": [[0, 2, \"Out\", 1, 0, \"In\"]], \"states\": \"000\"}", data);

            SelectGate(5); // Switch-gate
            Task.Delay(1).GetAwaiter().GetResult();

            IGate? button = Click(canv, 100, 150);
            IGate? button2 = Click(canv, 100, 250);
            IGate? button3 = Click(canv, 100, 350);
            Assert.NotNull(button);
            Assert.NotNull(button2);
            Assert.NotNull(button3);

            Move(button.SecretGetPin(0), gate.SecretGetPin(0));
            Move(button2.SecretGetPin(0), gate.SecretGetPin(1));
            Move(button3.SecretGetPin(0), gate2.SecretGetPin(1));

            SelectGate(7); // LightBulb-gate
            Task.Delay(1).GetAwaiter().GetResult();

            IGate? ball = Click(canv, 400, 300);
            Assert.NotNull(ball);

            Move(gate2.SecretGetPin(2), ball.SecretGetPin(0));

            var input = (Switch) button;
            var input2 = (Switch) button2;
            var input3 = (Switch) button3;
            var output = (LightBulb) ball;

            StringBuilder sb = new();
            for (int i = 0; i < 8; i++) {
                input.SetState((i & 4) > 0);
                input2.SetState((i & 2) > 0);
                input3.SetState((i & 1) > 0);
                if (i > 0) sb.Append('|');
                for (int tick = 0; tick < 5; tick++) {
                    Ticks(1);
                    sb.Append(output.GetState() ? '1' : '0');
                }
            }
            Assert.Equal("00000|00111|11000|00111|11000|00111|11011|11000", sb.ToString());
        }
    }
}