using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI.Studio;
using Studio;
using UnityEngine;

namespace BoxSelect.KKS
{
	[BepInPlugin(GUID, NAME, VERSION)]
	public class TestClass : BaseUnityPlugin
	{
		private const string GUID = "org.fox.test.kks";
		private const string NAME = "Test Plugin";
		private const string VERSION = "1.0";

		private void Awake()
		{
			ctrl = GameObject.FindObjectOfType<Studio.CameraControl>();
		}

		private Studio.CameraControl ctrl;

		Rect selectionRect = Rect.zero;
		Vector2 startPosition;
		bool isSelecting = false;

		private void Update()
		{
			// Start selection on mouse down
			if (Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.LeftAlt))
			{
				startPosition = Event.current.mousePosition;
				isSelecting = true;
			}

			// Update selection while mouse button is held and Alt is pressed
			if (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftAlt))
			{
				Vector2 currentMousePos = Event.current.mousePosition;

				// Calculate rect between start position and current position
				float width = currentMousePos.x - startPosition.x;
				float height = currentMousePos.y - startPosition.y;

				selectionRect = new Rect(
					width < 0 ? currentMousePos.x : startPosition.x,
					height < 0 ? currentMousePos.y : startPosition.y,
					Mathf.Abs(width),
					Mathf.Abs(height)
				);
			}

			// End selection when mouse button is released
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
			// Only draw if we have a valid rectangle
			if (selectionRect.width > 0 && selectionRect.height > 0)
			{
				GUI.color = new Color(1, 1, 1, 0.2f);
				GUI.DrawTexture(selectionRect, Texture2D.whiteTexture);
			}
		}

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

				if (viewportPosition.z > 0 &&
					viewportPosition.x >= viewportRect.x &&
					viewportPosition.x <= viewportRect.x + viewportRect.width &&
					viewportPosition.y >= viewportRect.y &&
					viewportPosition.y <= viewportRect.y + viewportRect.height)
				{
					selectedObjects.Add(guideObject.Value);
				}
			}

			guideObjectManager.selectObject = null;

			for (int i = 0; i < selectedObjects.Count; i++)
			{
				var selected = selectedObjects.ElementAt(i);
				// guideObjectManager.hashSelectObject.Add(selected);
				guideObjectManager.AddObject(selected);
			}
		}
	}
}