using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ChaCustom;
using HarmonyLib;
using MessagePack;
using Studio;
using Prototype;
using Prototype.UIElements;
using UnityEngine;
using Gradient = Prototype.UIElements.Gradient;
using Input = UnityEngine.Input;
using Object = UnityEngine.Object;
using Texture = Prototype.Texture;

namespace BoxSelect.KKS
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class BoxSelect : BaseUnityPlugin
    {
        private const string GUID = "org.fox.boxselect.kks";
        private const string NAME = "Test Plugin";
        private const string VERSION = "1.0";

        private Studio.CameraControl ctrl;
        private Rect selectionRect = Rect.zero;
        private Vector2 startPosition;
        private bool isSelecting;

        private ConfigEntry<string> s;
        private void Awake()
        {

            ctrl = FindObjectOfType<Studio.CameraControl>();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.LeftAlt))
            {
                startPosition = Event.current.mousePosition;
                isSelecting = true;
            }

            if (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftAlt))
            {
                Vector2 currentMousePos = Event.current.mousePosition;

                float width = currentMousePos.x - startPosition.x;
                float height = currentMousePos.y - startPosition.y;

                selectionRect = new Rect(
                    width < 0 ? currentMousePos.x : startPosition.x,
                    height < 0 ? currentMousePos.y : startPosition.y,
                    Mathf.Abs(width),
                    Mathf.Abs(height)
                );

                var t = nameof(ChaCustom.CustomBase.CustomSettingSave.Load);
            }


            if (Input.GetMouseButtonUp(0) && isSelecting)
            {
                isSelecting = false;
                GetObjects();
                selectionRect = Rect.zero;
            }

            ctrl.enabled = !isSelecting;
        }

        private bool sr = false;
        private Color t;
        private int i = 4;

        private Gradient gradient = new Gradient();
        private ColorPicker picker = new ColorPicker()
        {
            Quality = 4
        };
        private Dropdown dropdown = new Dropdown(new []{"1", "2", "3", "4", "5"});
        private MultiselectDropdown mdropdown = new MultiselectDropdown(new []{"1", "2", "3", "4", "5"});
        private Slider slider = new Slider(0, 0, 100);
        private void OnGUI()
        {
            Charts.DrawPieChart(new Rect(100, 350, 300, 300), new List<Charts.DataPoint>()
            {
                new Charts.DataPoint("1", 10, Color.white),
                new Charts.DataPoint("2", 20, Color.red),
                new Charts.DataPoint("3", 30, Color.green),
                new Charts.DataPoint("4", 40, Color.blue),
                new Charts.DataPoint("5", 50, Color.yellow),
            });;
            gradient.Draw(new Rect(100, 100, 300, 30));
            picker.Draw(new Rect(405, 100, 200, 200));
            dropdown.Draw(new Rect(405, 325, 200, 200));
            mdropdown.Draw(new Rect(610, 325, 200, 200));
            slider.Draw(new Rect(405, 550, 200, 20));
            if (selectionRect.width > 0 && selectionRect.height > 0)
            {
                GUI.color = new Color(1, 1, 1, 0.2f);
                GUI.DrawTexture(selectionRect, Texture2D.whiteTexture);
            }
        }

        /// <summary>
        /// Identifies and processes the objects within a rectangular selection area based on a viewport-relative boundary.
        /// Updates the state of the identified objects and registers the selected objects for further operations.
        /// </summary>
        /// <remarks>
        /// This method converts the screen-space selection rectangle into a viewport-space rectangle
        /// and then checks all candidate objects to determine if they fall within the selection area.
        /// Objects are managed and processed through the GuideObjectManager and their selection state is updated.
        /// </remarks>
        private void GetObjects()
        {
            Camera mainCamera = Camera.main;
            Rect viewportRect = new Rect(
                selectionRect.x / Screen.width,
                (Screen.height - selectionRect.y - selectionRect.height) / Screen.height,
                selectionRect.width / Screen.width,
                selectionRect.height / Screen.height);
            var guideObjectManager = Singleton<GuideObjectManager>.Instance;
            var objects = guideObjectManager.dicGuideObject;

            HashSet<GuideObject> selectedObjects = new HashSet<GuideObject>();
            foreach (var guideObject in objects)
            {
                guideObject.Value.isActive = false;
                if (guideObject.Value.transformTarget == null)
                    continue;


                Vector3 objectPosition = guideObject.Value.transformTarget.position;
                Vector3 viewportPosition = mainCamera.WorldToViewportPoint(objectPosition);

                if (Point.IsPointInRect(viewportPosition, viewportRect) | Point.IsPointInRect(viewportPosition, Rect.zero))
                {
                    selectedObjects.Add(guideObject.Value);
                }
            }

            RegisterSelectedObjects(guideObjectManager, selectedObjects);
        }

        private static void RegisterSelectedObjects(GuideObjectManager guideObjectManager, HashSet<GuideObject> selectedObjects)
        {
            guideObjectManager.selectObject = null;

            for (int i = 0; i < selectedObjects.Count; i++)
            {
                var selected = selectedObjects.ElementAt(i);
                guideObjectManager.AddObject(selected);
            }
        }
    }
}