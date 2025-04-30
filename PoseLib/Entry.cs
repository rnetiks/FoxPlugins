using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Autumn;
using BepInEx.Configuration;
using SmartRectV0;
using Studio;
using UnityEngine;

namespace PoseLib.KKS
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public partial class Entry : BaseUnityPlugin
    {
        #region Properties

        private const string GUID = "org.fox.poselib";
        private const string NAME = "PoseLibrary";
        private const string VERSION = "1.0.0";

        private ConfigEntry<KeyboardShortcut> openUIKey;

        private GUIStyle mainWindowStyle;
        private GUIStyle fontStyle;
        private bool openUI;

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
        private OCIChar[] selectedCharacters;

        #endregion

        private void Awake()
        {
            EnsureDirectoriesExist();
            openUIKey = Config.Bind("General", "Open Window", new KeyboardShortcut(KeyCode.N, KeyCode.RightControl));
            var width = Screen.width / 2f;
            var height = Screen.height / 1.3f;
            clientRect = new Rect(100, 100, width, height);
            LoadTextures(width, height);
            CreateGuiStyles();
            UpdateWindow();
        }

        private void CreateGuiStyles()
        {
            mainWindowStyle = new GUIStyle()
            {
                normal =
                {
                    background = backgroundImage
                },
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };

            fontStyle = new GUIStyle()
            {
                normal =
                {
                    textColor = Color.black
                },
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }

        private static void EnsureDirectoriesExist()
        {
            if (!Directory.Exists("Poses"))
                Directory.CreateDirectory("Poses");
            if (!Directory.Exists("Fox-Textures"))
                Directory.CreateDirectory("Fox-Textures");
        }

        private void Update()
        {
            if (openUIKey.Value.IsDown())
                openUI = !openUI;
        }

        private void LoadTextures(float width, float height)
        {
            if (!File.Exists("./wb.png"))
                TextureFactory.Create((int)width, (int)height)
                    .BackgroundColor(220, 220, 220, 255)
                    .Border(2, new Color32(0, 119, 255, 255))
                    .Opacity(0.7f)
                    .Save("./wb.png");
            backgroundImage = TextureFactory.Load("./wb.png");
        }

        private void OnGUI()
        {
            if (!openUI)
                return;
            if (openSaveWindow)
            {
                RenderSaveWindowUI();
                return;
            }

            if (clientRect.Contains(Event.current.mousePosition))
                Input.ResetInputAxes();
            clientRect = GUI.Window(31, clientRect, Window, "PoseLibrary", mainWindowStyle);
        }

        // ReSharper restore Unity.ExpensiveCode

        string chosenFilename = String.Empty;
        string chosenTags = String.Empty;
        Dictionary<string, ChangeAmount> chosenData = new Dictionary<string, ChangeAmount>();

        private TextureElement screenshot;

        private void RenderSaveWindowUI()
        {
            var CenterX = Screen.width / 2f;
            var CenterY = Screen.height / 2f;
            var smartRect = new SmartRect(CenterX - 200, CenterY - 150, 400, 300);
            GUI.DrawTexture(smartRect, backgroundImage);
            if (GUI.Button(new SmartRect(smartRect).SetWidth(30).SetHeight(30).MoveToEndX(smartRect, 30), "X"))
            {
                chosenFilename = string.Empty;
                chosenTags = string.Empty;
                openSaveWindow = false;
                chosenData.Clear();
            }

            if (GUI.Button(smartRect.MoveToEndX(smartRect, 30).SetWidth(30).SetHeight(30), "X"))
            {
                openSaveWindow = false;
            }

            GUI.DrawTexture(new Rect(Screen.width - 300, Screen.height - 300, 300, 300), screenshot);

            var lRects = new SmartRect(CenterX - 150, CenterY - 70, 300, 20);
            GUI.Label(lRects, "Filename", fontStyle);
            chosenFilename = GUI.TextArea(lRects.NextRow(), chosenFilename);
            GUI.Label(lRects.NextRow(), "Tags (Not implemented yet)", fontStyle);
            chosenTags = GUI.TextArea(lRects.NextRow(), chosenTags);
            lRects.NextRow().BeginHorizontal(2);
            if (GUI.Button(lRects, "Take Screenshot"))
            {
                openUI = false;
                screenshot.LoadScreen();
                var min = Mathf.Min(screenshot.Width, screenshot.Height);
                var max = Mathf.Max(screenshot.Width, screenshot.Height);

                float x = 0;
                float y = 0;

                if (screenshot.Width > screenshot.Height)
                {
                    x = (screenshot.Width - min) / 2;
                }
                else
                {
                    y = (screenshot.Height - min) / 2;
                }

                screenshot.Crop(new Rect(x, y, min, min));
                screenshot.Scale(256, 256);
                openUI = true;
            }

            if (GUI.Button(lRects.NextColumn(), "Save") && !IsEmptyOrWhitespace(chosenFilename))
            {
                if (!Directory.Exists("Poses"))
                    Directory.CreateDirectory("Poses");
                try
                {
                    SaveFile("Poses/" + chosenFilename + ".png", chosenData);
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }

                chosenFilename = string.Empty;
                chosenTags = string.Empty;
                openSaveWindow = false;
                chosenData.Clear();
                UpdateWindow();
            }
        }

        bool IsEmptyOrWhitespace(string value)
        {
            if (value == null || value.Length == 0)
                return true;

            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsWhiteSpace(value[i]))
                    return false;
            }

            return true;
        }


        private void Window(int id)
        {
            SmartRect rect = new SmartRect(xOffset, yOffset, 100, 20, 5, 5);
            selectedCharacters = KKAPI.Studio.StudioAPI.GetSelectedCharacters().ToArray();
            if (selectedCharacters.Length > 0)
            {
                if (GUI.Button(rect, "Save Pose"))
                {
                    chosenData = GetFkData(selectedCharacters[0]);
                    openSaveWindow = true;
                    screenshot = TextureFactory.Create(256, 256).BackgroundColor(255, 255, 255, 255);
                }

                rect.NextColumn();
            }
            else
            {
                GUI.Label(new Rect(0, 0, clientRect.width, clientRect.height), "No character selected", fontStyle);
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
                if (foundFiles.Count <= 0)
                {
                    GUI.Label(new Rect(0, 0, clientRect.width, clientRect.height), "No poses found", fontStyle);
                }

                int idx = 0;
                foreach (var file in foundFiles)
                {
                    GUI.DrawTexture(imageRect, file.Value);
                    if (imageRect.ToRect().Contains(Event.current.mousePosition))
                    {
                        if (GUI.Button(
                                new SmartRect(imageRect).SetWidth(Mathf.Max(imageRect.Width * 0.33333f, 50))
                                    .SetHeight(30), "Delete"))
                        {
                            File.Delete(file.Key);
                            UpdateWindow();
                            break;
                        }

                        var smartRect = new SmartRect(imageRect);
                        smartRect.Width = Mathf.Max(imageRect.Width * 0.33333f, 50);
                        smartRect.Height = 30;
                        smartRect.MoveToEndY(imageRect, smartRect.Height);
                        if (GUI.Button(smartRect, "Load"))
                        {
                            try
                            {
                                var poseData = LoadFile(file.Key);
                                foreach (var selectedCharacter in selectedCharacters)
                                {
                                    SetFkData(selectedCharacter, poseData);
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.LogError(e);
                            }
                        }

                        smartRect.MoveToEndX(imageRect, smartRect.Width);
                        // GUI.Button(smartRect, "Share");
                    }

                    imageRect.NextColumn();
                    if (++idx % xPrevs == 0)
                    {
                        imageRect.NextRow();
                    }
                }
            }

            var width = 30;
            float footerWidth = 3 * width + 2 * 5;
            var footerX = (clientRect.width - footerWidth) * 0.5f;
            SmartRect footer = new SmartRect(footerX, clientRect.height - 25, width, 20, 5, 5);

            var tmpPage = page;
            if (GUI.Button(footer, "<"))
            {
                page = Math.Max(page - 1, 1);
            }

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

        private Dictionary<string, Texture2D> foundFiles = new Dictionary<string, Texture2D>();

        private void UpdateWindow()
        {
            foundFiles.Clear();
            var files = Directory.GetFiles("Poses/", "*.png");
            var idx = 0;
            var offset = page * 10 - 10;

            for (int i = offset; i < files.Length; i++)
            {
                string file = files[i];
                if (idx++ >= 10) break;

                if (searchQuery.Length > 0)
                {
                    if (file.ToLower().Contains(searchQuery.ToLower()))
                    {
                        Texture2D loadedTexture = TextureFactory.Load(file);
                        foundFiles.Add(file, loadedTexture);
                    }
                }
                else
                {
                    Texture2D loadedTexture = TextureFactory.Load(file);
                    foundFiles.Add(file, loadedTexture);
                }
            }
        }
    }
}