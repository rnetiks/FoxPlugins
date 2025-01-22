using System;
using System.Collections;
using System.Linq;
using JetBrains.Annotations;
using PrismaLib.Settings;
using PrismaLib.Settings.Type;
using UnityEngine;

namespace Autumn
{
    public class UIManager : MonoBehaviour
    {
        private static GUIBase[] activeGUIs = new GUIBase[0];
        internal static FloatSetting HUDScaleGUI = new FloatSetting("HUDScaleGUI", 1f);
        internal static BoolSetting HUDAutoScaleGUI = new BoolSetting("HUDAutoScaleGUI", true);
        internal static FloatSetting LabelScale = new FloatSetting("LabelScale", 1f);
        private static Action _onAwakeAdds = delegate { };
        private static Action _onAwakeRms = delegate { };

        [CanBeNull]
        public static UIManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError(
                    "There should be only one instance of \"UIManager\". Please make sure yo spawn it just once.");
                DestroyImmediate(this);
                return;
            }

            DontDestroyOnLoad(this);
            Instance = this;
            _onAwakeAdds();
            _onAwakeRms();
            _onAwakeAdds = delegate { };
            _onAwakeRms = delegate { };
        }

        public static bool Disable(GUIBase gui)
        {
            if (!gui.IsActive)
            {
                return false;
            }

            if (Instance == null)
            {
                _onAwakeRms += delegate { Disable(gui); };
                return false;
            }

            lock (activeGUIs)
            {
                var wasLast = activeGUIs
                    .OrderBy(g => g.Layer)
                    .LastOrDefault() == gui;

                var oldCount = activeGUIs.Length;

                activeGUIs = activeGUIs
                    .Where(x => x != gui)
                    .ToArray();

                var wasRemoved = oldCount != activeGUIs.Length;

                if (wasRemoved)
                {
                    gui.Disable();
                }

                if (activeGUIs.Length == 0)
                {
                    activeGUIs = Array.Empty<GUIBase>();
                    return wasRemoved;
                }

                if (!wasLast)
                {
                    UpdateDepths();
                }

                return wasRemoved;
            }
        }

        public static bool Enable(GUIBase gui)
        {
            if (gui.IsActive)
            {
                return false;
            }

            if (Instance == null)
            {
                _onAwakeAdds += delegate { Enable(gui); };
                return false;
            }

            lock (activeGUIs)
            {
                if (activeGUIs == null || activeGUIs.Length == 0)
                {
                    activeGUIs = new[] { gui };
                    gui.Drawer.Enable();
                    UpdateDepths();
                    return true;
                }

                if (activeGUIs.Any(t => t == gui))
                {
                    return false;
                }

                var list = activeGUIs.ToList();
                list.Add(gui);
                activeGUIs = list
                    .OrderBy(x => x.Layer)
                    .ToArray();

                if (activeGUIs.Last() == gui)
                {
                    if (activeGUIs.Length >= 2)
                    {
                        gui.Drawer.Enable(activeGUIs[activeGUIs.Length - 2].Drawer.Depth - 1);
                    }
                    else
                    {
                        gui.Drawer.Enable();
                        UpdateDepths();
                    }
                }
                else
                {
                    gui.Drawer.Enable();
                    UpdateDepths();
                }
                
                return true;
            }
        }

        private static void UpdateDepths()
        {
            var depth = activeGUIs.Length + 10;
            foreach (var item in activeGUIs)
            {
                item.Drawer.Depth = depth--;
            }
        }

        public static void SetParent(UIBase ui)
        {
            Transform uiTransform;
            Vector3 instancePosition;
            if (Instance == null)
            {
                _onAwakeAdds += delegate
                {
                    uiTransform = ui.transform;
                    instancePosition = Instance.transform.position;
                    uiTransform.SetParent(Instance.transform);
                    uiTransform.position = new Vector3(instancePosition.x, instancePosition.y, instancePosition.z);
                };
                return;
            }
            uiTransform = ui.transform;
            instancePosition = Instance.transform.position;

            if (!Instance.gameObject.activeInHierarchy)
            {
                Instance.gameObject.SetActive(true);
            }

            uiTransform.SetParent(Instance.transform);
            uiTransform.position = new Vector3(instancePosition.x, instancePosition.y, instancePosition.z);
        }

        private static IEnumerator WaitAndEnable(GUIBase baseG)
        {
            yield return new WaitForEndOfFrame();
            baseG.DisableImmediate();
            baseG.OnUpdateScaling();
            yield return new WaitForEndOfFrame();
            baseG.EnableImmediate();
        }

        public static void UpdateGUIScaling()
        {
            Style.UpdateScaling();
            lock (activeGUIs)
            {
                foreach (var baseg in GUIBase.AllBases)
                {
                    if (baseg.IsActive && Instance != null)
                    {
                        Instance.StartCoroutine(WaitAndEnable(baseg));
                        continue;
                    }

                    baseg.OnUpdateScaling();
                }
            }
        }
    }
}