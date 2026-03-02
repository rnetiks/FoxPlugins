using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using Studio;
using UnityEngine;
using Input = UnityEngine.Input;

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


        private void OnGUI()
        {
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

                if (viewportRect.Contains(viewportPosition))
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