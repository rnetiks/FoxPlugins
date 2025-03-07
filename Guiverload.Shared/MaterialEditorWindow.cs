using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SmartRectV0;
using UnityEditor;
using UnityEngine;

namespace Guiverload.KKS
{
    public class MaterialEditorWindow
    {
        private TestWindow _testWindow;

        public MaterialEditorWindow(TestWindow testWindow)
        {
            _testWindow = testWindow;
        }

        private KeyValuePair<Material, Renderer> _selectedMaterial;
        private string _searchQuery = string.Empty;
        private Vector2 _scrollPosition = Vector2.zero;
        private KeyValuePair<string, KeyValuePair<Material, Renderer>>[] _sortedMaterials;
        private static bool _previewOpen;
        private static bool _surfaceOpen;
        private static bool _settingsOpen;

        private void Reload()
        {
            _sortedMaterials = getMaterials().Where(E => E.Key.ToLower().Contains(_searchQuery.ToLower())).ToArray();
        }

        public void MaterialWindow(SmartRect re2)
        {
            Reload();
            GUI.Label(re2.MoveX(10),
                _selectedMaterial.Key == null
                    ? "Materials"
                    : $"Material [{_selectedMaterial.Key.name.Replace("(Instance)", string.Empty).Trim()}]");
            re2.NextRow();
            FastUI.TextField(ref _searchQuery, re2);
            re2.NextRow();
            var position = new Rect(re2.X, re2.Y, re2.Width, Mathf.Min(300, _sortedMaterials.Length * 25));
            if (_sortedMaterials.Length > 0)
            {
                _scrollPosition = GUI.BeginScrollView(
                    position,
                    _scrollPosition,
                    new Rect(0, 0, re2.Width - 30, Mathf.Max(1, (re2.Height + 5) * _sortedMaterials.Length)));
                SmartRect nr = new SmartRect(0, 0, re2.Width, re2.Height);
                foreach (var material in _sortedMaterials)
                {
                    if (!material.Key.Contains(_searchQuery))
                        continue;
                    FastUI.Button(nr, material.Key.Replace("(Instance)", ""), () => _selectedMaterial = material.Value);
                    nr.NextRow();
                }

                GUI.EndScrollView();
                re2.MoveY(position.height + 10);
            }

            if (_selectedMaterial.Key == null)
                return;
            Preview(re2);
            Surface(re2);
            Settings(re2);
        }

        private void Settings(SmartRect re2)
        {
            FastUI.Spoiler(re2, "Settings", () =>
            {
                if (_selectedMaterial.Key == null)
                    return;
                re2.Width = re2.Width - 20;
                FastUI.Button(re2.NextRow().MoveX(10),
                    !_selectedMaterial.Value.enabled ? "Enable Renderer" : "Disabled Renderer",
                    () => { _selectedMaterial.Value.enabled = !_selectedMaterial.Value.enabled; });
                FastUI.Button(re2.NextRow().MoveX(10),
                    !_selectedMaterial.Key.enableInstancing ? "Enable Instancing" : "Disable Instancing",
                    () => _selectedMaterial.Key.enableInstancing = !_selectedMaterial.Key.enableInstancing);
                var selectedMaterialName = _selectedMaterial.Key.name;
                FastUI.TextField(ref selectedMaterialName, re2.NextRow().MoveX(10));
                _selectedMaterial.Key.name = selectedMaterialName;

                re2.ResetX();
            }, ref _settingsOpen);
            re2.NextRow();
        }

        private static void Surface(SmartRect re2)
        {
            // TODO
            FastUI.Spoiler(re2, "Surfaces", () => { GUI.Label(re2.NextRow(), "Surfaces is Open"); }, ref _surfaceOpen);
            re2.NextRow();
        }

        private static void Preview(SmartRect re2)
        {
            // TODO
            FastUI.Spoiler(re2, "Preview", () => { GUI.Label(re2.NextRow(), "Preview is Open"); }, ref _previewOpen);
            re2.NextRow();
        }

        private List<KeyValuePair<string, KeyValuePair<Material, Renderer>>> getMaterials()
        {
            List<KeyValuePair<string, KeyValuePair<Material, Renderer>>> materials =
                new List<KeyValuePair<string, KeyValuePair<Material, Renderer>>>();

            foreach (var selectedCharacter in KKAPI.Studio.StudioAPI.GetSelectedCharacters())
            {
                GatherMaterials(selectedCharacter.guideObject.transformTarget, materials);
            }

            foreach (var objectCtrlInfo in KKAPI.Studio.StudioAPI.GetSelectedObjects())
            {
                GatherMaterials(objectCtrlInfo.guideObject.transformTarget, materials);
            }

            // var array = materials.ToArray();
            // Array.Sort(array, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
            return materials;
        }

        private void GatherMaterials(Transform obj, List<KeyValuePair<string, KeyValuePair<Material, Renderer>>> materials)
        {
            if (obj == null)
                return;

            
            foreach (var r in obj.GetComponentsInChildren<Renderer>())
            {
                materials.AddRange(r.sharedMaterials.Select(m => new KeyValuePair<string, KeyValuePair<Material, Renderer>>(m.name, new KeyValuePair<Material, Renderer>(m, r))));
            }
        }
    }
}