using Avalonia.Controls;
using RGR.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace RGR.Models {
    public class FileHandler {
        readonly string AppData;
        readonly List<Project> projects = new();
        readonly List<string> project_paths = new();
        readonly Dictionary<string, Project> proj_dict = new();

        public FileHandler() {
            string app_data = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            app_data = Path.Combine(app_data, "RGR");
            if (!Directory.Exists(app_data)) Directory.CreateDirectory(app_data);
            AppData = app_data;
            LoadProjectList();
        }



        private void AddProject(Project proj) {
            if (proj.FileDir == null || proj.FileName == null) return;

            var path = Path.Combine(proj.FileDir, proj.FileName);
            if (proj_dict.ContainsKey(path)) return;

            proj_dict[path] = proj;
            projects.Add(proj);
        }
        private static string GetProjectFileName(string dir) {
            int n = 0;
            while (true) {
                string name = "proj_" + ++n + ".xml";
                if (!File.Exists(Path.Combine(dir, name))) return name;
            }
        }



        public Project CreateProject() {
            var proj = new Project(this);
            // projects.Add(proj); Перенесено в AppendProject
            return proj;
        }
        private Project? LoadProject(string dir, string fileName) {
            try {
                var obj = auxiliary.Xml2obj(File.ReadAllText(Path.Combine(dir, fileName))) ?? throw new DataException("Не верная структура XML-файла проекта!");
                var proj = new Project(this, dir, fileName, obj);
                AddProject(proj);
                return proj;
            } catch (Exception e) { Log.Write("Неудачная попытка загрузить проект:\n" + e); }
            return null;
        }
        private Project? LoadProject(string path) {
            var s_arr = path.Split(Path.DirectorySeparatorChar).ToList();
            var name = s_arr[^1];
            s_arr.RemoveRange(s_arr.Count - 1, 1);
            var dir = Path.Combine(s_arr.ToArray());

            return LoadProject(dir, name);
        }
        private void LoadProjectList() {
            var file = Path.Combine(AppData, "project_list.xml");
            if (!File.Exists(file)) return;

            object data;
            try { data = auxiliary.Xml2obj(File.ReadAllText(file)) ?? throw new DataException("Не верная структура XML-файла списка проектов!"); } catch (Exception e) { Log.Write("Неудачная попытка загрузить список проектов:\n" + e); return; }

            if (data is not List<object> @arr) { Log.Write("В списке проектов на верхнем уровне ожидалось увидеть список"); return; }
            foreach (var path in @arr) {
                if (path is not string @str) { Log.Write("Один из путей списка проектов - не строка: " + path); continue; }
                project_paths.Add(@str);
                LoadProject(@str);
            }
        }



        internal static void SaveProject(Project proj) {
            var dir = proj.FileDir;
            if (dir == null) return;

            var data = auxiliary.Obj2xml(proj.Export());
            var name = proj.FileName;
            name ??= GetProjectFileName(dir);
            proj.FileName = name;

            var path = Path.Combine(dir, name);
            File.WriteAllText(path, data);
        }
        private void SaveProjectList() {
            var file = Path.Combine(AppData, "project_list.xml");
            var content = auxiliary.Obj2xml(project_paths);
            File.WriteAllText(file, content);
        }

        internal Project[] GetSortedProjects() {
            projects.Sort();
            return projects.ToArray();
        }
        internal void AppendProject(Project proj) {
            if (proj.FileDir == null || proj.FileName == null) return;

            var path = Path.Combine(proj.FileDir, proj.FileName);
            if (project_paths.Contains(path)) return;

            project_paths.Add(path);
            AddProject(proj);
            SaveProjectList();
        }



        internal static string? RequestProjectPath(Window parent) {
            var dlg = new OpenFolderDialog {
                Title = "Выберите папку, куда надо сохранить новый проект"
            };
            var task = dlg.ShowAsync(parent);
            return task.GetAwaiter().GetResult();
        }
        internal Project? SelectProjectFile(Window parent) {
            var dlg = new OpenFileDialog {
                Title = "Выберите файл с проектом (proj_*.xml), который нужно открыть"
            };
            dlg.Filters?.Add(new FileDialogFilter() { Name = "XML Files", Extensions = { "xml" } });
            dlg.Filters?.Add(new FileDialogFilter() { Name = "All Files", Extensions = { "*" } });
            dlg.AllowMultiple = false;

            var task = dlg.ShowAsync(parent);
            var res = task.GetAwaiter().GetResult();
            if (res == null) return null;

            var path = res[0];
            if (!proj_dict.TryGetValue(path, out var proj)) {
                proj = LoadProject(path);
                if (proj != null) AppendProject(proj);
            }
            return proj;
        }
    }
}
