using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using Autumn;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Public.Patching;
using KKAPI.Utilities;
using MessagePack;
using ParadoxNotion.Serialization;
using Sirenix.Serialization;
using SmartRectV0;
using Studio;
using UnityEngine;

namespace PoseLib.KKS
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Entry : BaseUnityPlugin
    {
        private const string GUID = "org.fox.poselib";
        private const string NAME = "PoseLibrary";
        private const string VERSION = "1.0.0";
        public static ManualLogSource _logger;

        private void Awake()
        {
            _logger = Logger;
            var width = Screen.width / 2f;
            var height = Screen.height / 1.3f;
            clientRect = new Rect(100, 100, width, height);
            LoadTextures(width, height);
            mainWindowStyle = new GUIStyle()
            {
                normal =
                {
                    background = backgroundImage
                },
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };
        }

        private void LoadTextures(float width, float height)
        {
            placeholder = TextureFactory
                .Load("./template.png",
                    path => TextureFactory.Load(
                            @"C:\Users\aepod\Desktop\ALL NAIV4\fox ears, fox tail, fox girl, ruins, by nagishiro mito, close-up, headshot, glow s-1340840478.png")
                        .Scale(128, -1)
                        .Save(path));
            backgroundImage = TextureFactory.Load("./wb.png", path => TextureFactory.Create((int)width, (int)height)
                .BackgroundColor(220, 220, 220, 255)
                .Border(2, new Color32(0, 119, 255, 255))
                .Save(path));
        }
        
        private GUIStyle mainWindowStyle;

        private const int xOffset = 5;
        private const int yOffset = 20;

        TextureElement placeholder;
        private Texture2D backgroundImage;

        private string searchQuery = string.Empty;
        private string tmpSearch = string.Empty;
        
        private Rect clientRect;

        private int xPrevs = 5;
        private int page = 1;
        
        private float searchCooldown;

        private bool openSaveWindow;
        
        private void OnGUI()
        {
            if (openSaveWindow)
            {
                var CenterX = Screen.width / 2f;
                var CenterY = Screen.height / 2f;
                var smartRect = new SmartRect(CenterX - 200, CenterY - 150, 400, 300);
                GUI.DrawTexture(smartRect, backgroundImage);
                if (GUI.Button(smartRect.MoveToEndX(smartRect, 30).SetWidth(30).SetHeight(30), "X"))
                {
                    openSaveWindow = false;
                }
                var lRects = new SmartRect(CenterX - 150, CenterY - 30, 300, 20);
                GUI.TextArea(lRects, "Name:");
                GUI.TextArea(lRects.NextRow(), "Tags:");
                lRects.NextRow().BeginHorizontal(2);
                GUI.Button(lRects, "Take Screenshot");
                GUI.Button(lRects.NextColumn(), "Save");
                return;
            }
            if (clientRect.Contains(Event.current.mousePosition))
                Input.ResetInputAxes();
            clientRect = GUI.Window(31, clientRect, Window, "PoseLibrary", mainWindowStyle);
        }


        private Dictionary<Transform, GuideObject> GetBoneData()
        {
            GuideObjectManager instance = Singleton<GuideObjectManager>.Instance;
            Dictionary<Transform, GuideObject> kvp = instance.dicGuideObject;

            return kvp;
        }

        private void Window(int id)
        {
            SmartRect rect = new SmartRect(xOffset, yOffset, 100, 20, 5, 5);
            var selectedCharacters = KKAPI.Studio.StudioAPI.GetSelectedCharacters().ToArray();
            if (selectedCharacters.Length > 0)
            {
                if (GUI.Button(rect, "Save Pose"))
                {
                    openSaveWindow = true;
                }

                rect.NextColumn();
            }

            searchQuery = GUI.TextField(rect.SetWidth(200), searchQuery);
            if (GUI.Button(rect.MoveX(205).SetWidth(20), "+"))
            {
                xPrevs = Math.Min(xPrevs + 1, 8);
            }

            if (GUI.Button(rect.MoveX(25).SetWidth(20), "-"))
            {
                xPrevs = Math.Max(xPrevs - 1, 3);
            }

            if (selectedCharacters.Length > 0)
            {
                var previewSize = (clientRect.width - (xOffset * xPrevs + 5)) / xPrevs;
                var imageRect = new SmartRect(xOffset, rect.NextRow().Y, previewSize, previewSize, xOffset, 5);
                for (int i = 0; i < 20; i++)
                {
                    GUI.DrawTexture(imageRect, placeholder.GetTexture());
                    if (imageRect.ToRect().Contains(Event.current.mousePosition))
                    {
                        var smartRect = new SmartRect(imageRect);
                        smartRect.Width = Mathf.Max(imageRect.Width * 0.33333f, 50);
                        smartRect.Height = 30;
                        smartRect.MoveToEndY(imageRect, smartRect.Height);
                        GUI.Button(smartRect, "Load");
                        smartRect.MoveToEndX(imageRect, smartRect.Width);
                        GUI.Button(smartRect, "Share");
                    }

                    imageRect.NextColumn();
                }
            }

            var width = 30;
            float footerWidth = 3 * width + 2 * 5;
            var footerX = (clientRect.width - footerWidth) * 0.5f;
            SmartRect footer = new SmartRect(footerX, clientRect.height - 25, width, 20, 5, 5);

            if (GUI.Button(footer, "<"))
            {
                page = Math.Max(page - 1, 1);
            }

            var tmpPage = page;
            page = int.Parse(GUI.TextField(footer.NextColumn(), page.ToString()));

            if (GUI.Button(footer.NextColumn(), ">"))
            {
                page = Math.Min(page + 1, 100);
            }

            searchCooldown -= Time.deltaTime;
            if (tmpSearch != searchQuery && searchCooldown <= 0)
            {
                page = 1;
                tmpSearch = searchQuery;
                searchCooldown = 2f;
                UpdateWindow();
                return;
            }

            if (tmpPage != page)
                UpdateWindow();
            GUI.DragWindow();
        }

        private Dictionary<string, float[]> GetPoseData(OCIChar c)
        {
            Dictionary<string, float[]> data = new Dictionary<string, float[]>();
            var instance = Singleton<GuideObjectManager>.Instance;
            foreach (var boneData in instance.dicGuideObject)
            {
                List<float> q = new List<float>()
                {
                    boneData.Key.position.x,
                    boneData.Key.position.y,
                    boneData.Key.position.z,

                    boneData.Key.rotation.x,
                    boneData.Key.rotation.y,
                    boneData.Key.rotation.z,
                    boneData.Key.rotation.w,

                    boneData.Key.localScale.x,
                    boneData.Key.localScale.y,
                    boneData.Key.localScale.z,
                };


                data.Add(boneData.Key.name, q.ToArray());
            }

            return data;
        }

        private void UpdateWindow()
        {
            Logger.LogError(
                $"Checking for poses on page {page} with query {(searchQuery.Length > 0 ? searchQuery : "*")}");
        }
    }
}