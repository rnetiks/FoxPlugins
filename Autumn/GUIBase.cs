using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autumn.Animation;
using Autumn.Attributes;
using Autumn.Localization;
using UnityEngine;

namespace Autumn
{
    public abstract class GUIBase
    {
        private static readonly List<int> usedLayers = new List<int>();
        private static Texture2D cachedEmptyTexture;
        private static int maxLayer;
        private readonly Dictionary<string, Texture2D> textureCache;
        protected GUIAnimation animator;
        public readonly string Name;
        public Action OnGUI = () => { };
        protected Locale locale { get; }
        public static List<GUIBase> AllBases { get; private set; }

        [AutumnTexture2D("transparent")]
        public static Texture2D EmptyTexture
        {
            get
            {
                if (cachedEmptyTexture != null)
                    return cachedEmptyTexture;
                
                cachedEmptyTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                cachedEmptyTexture.SetPixel(0, 0, Colors.Empty);
                cachedEmptyTexture.Apply();
                
                return cachedEmptyTexture;
            }
        }

        public string Directory { get; }
        public GUIDrawer Drawer { get; }
        public bool IsActive { get; private set; }
        public int Layer { get; }

        ~GUIBase()
        {
            lock (usedLayers)
            {
                int indexToRemove = -1;
                for (int i = 0; i < usedLayers.Count; i++)
                {
                    if (usedLayers[i] != Layer) continue;
                    indexToRemove = i;
                    break;
                }

                if (indexToRemove >= 0)
                    usedLayers.RemoveAt(indexToRemove);
                UpdateMaxLayer();
            }

            if (AllBases.Contains(this))
                AllBases.Remove(this);
        }

        public GUIBase(string name, int layer = -1)
        {
            Name = name;
            Layer = GetLayer(layer, name);
            Directory = Application.dataPath + "/Resources/" + Name + "/";
            if (!System.IO.Directory.Exists(Directory))
                System.IO.Directory.CreateDirectory(Directory);
            textureCache = new Dictionary<string, Texture2D>();
            locale = new Locale(name);
            animator = new NoneAnimation(this);
            if (AllBases == null)
                AllBases = new List<GUIBase>();
            AllBases.Add(this);
            Drawer = new GUIDrawer(this);
        }

        private static int GetLayer(int layerToSet, string name)
        {
            lock (usedLayers)
            {
                if (layerToSet >= 0)
                {
                    if (usedLayers.Contains(layerToSet))
                    {
                        Debug.LogError(
                            $"Attempted to initialize GUIBase with layer that already exists. Layer: {layerToSet}, GUIBase: {name}");
                        return GetLayer(++layerToSet, name);
                    }

                    usedLayers.Add(layerToSet);
                    UpdateMaxLayer();
                    return layerToSet;
                }

                maxLayer = usedLayers.Max();
                usedLayers.Add(++maxLayer);
                return maxLayer;
            }
        }

        private static void UpdateMaxLayer() => maxLayer = usedLayers.Concat(new[] { -1 }).Max();

        protected virtual void OnDisable()
        {
        }

        protected virtual void OnEnable()
        {
        }

        protected internal abstract void Draw();

        public static Texture2D LoadTexture(GUIBase _base, string name, string ext = "png")
        {
            return _base.LoadTexture(name, ext);
        }

        public void ClearCache()
        {
            textureCache.Clear();
        }

        public void Disable()
        {
            if (!IsActive)
                return;
            OnGUI = animator.StartClose(DisableImmediate);
        }

        public void DisableImmediate()
        {
            if (!UIManager.Disable(this)) return;
            
            Drawer.Disable();
            OnDisable();
            locale.Unload();
            IsActive = false;
            OnGUI = null;
        }

        public void Enable()
        {
            if (IsActive)
                return;
            if (UIManager.Enable(this))
            {
                locale.Load();
                OnEnable();
                OnGUI = animator.StartOpen(() => { });
                IsActive = true;
            }
        }

        public void EnableImmediate()
        {
            if (!UIManager.Enable(this)) return;
            
            locale.Load();
            OnEnable();
            IsActive = true;
            OnGUI = Draw;
        }

        public void EnableNext(GUIBase next)
        {
            if (!IsActive)
                return;
            OnGUI = animator.StartClose(() =>
            {
                if (!UIManager.Disable(this)) return;
                
                OnDisable();
                locale.Unload();
                IsActive = false;
                next.Enable();
            });
        }

        public Texture2D LoadTexture(string namebase, string ext)
        {
            if (textureCache.TryGetValue(namebase, out Texture2D res) && res != null)
            {
                return res;
            }

            string name = namebase;
            bool error = false;
            if (ext == string.Empty)
            {
                if (!name.EndsWith(".png") && !name.EndsWith(".jpg") && !name.EndsWith(".jpeg"))
                {
                    error = true;
                }
            }
            else
            {
                if (!ext.Equals("png") && !ext.Equals("jpg") && !ext.Equals("jpeg"))
                {
                    error = true;
                }

                name += "." + ext;
            }

            if (error)
            {
                Debug.LogError("You should use png, jpg or jpeg extensions for loading Texture2D");
                return Texture2D.blackTexture;
            }

            string path = Directory + name;
            if (!File.Exists(path))
            {
                Debug.LogError($"File what you are trying to load doesnt't exist: \"{path}\"");
                return Texture2D.blackTexture;
            }

            res = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            res.LoadImage(File.ReadAllBytes(path));
            res.Apply();
            textureCache.Add(namebase, res);
            return res;
        }

        public virtual void OnUpdateScaling()
        {
        }

        /// <summary>
        /// Calls every frame
        /// </summary>
        public virtual void Update()
        {
        }
    }
}