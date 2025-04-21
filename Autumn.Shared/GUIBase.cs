using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autumn.Animation;
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

        /// <summary>
        /// The "Name" property represents the identifier or name of the current GUIBase instance.
        /// This property is used to uniquely distinguish between different GUIBase elements within the application.
        /// It is typically assigned during instance initialization and remains immutable throughout the lifecycle of the object.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The "OnGUI" delegate represents an action to be executed during the GUI rendering phase.
        /// This property can be assigned custom GUI rendering logic, enabling dynamic updates to the GUI's visual state.
        /// It is typically called during the Unity OnGUI method for the associated object.
        /// </summary>
        public Action OnGUI = () => { };
        protected Locale locale { get; }

        /// <summary>
        /// The "AllBases" property holds a static collection of all existing instances of the GUIBase class.
        /// This property enables centralized access to and management of all active GUIBase objects within the application.
        /// It is automatically updated when instances are created or destroyed.
        /// </summary>
        public static List<GUIBase> AllBases { get; private set; }

        /// <summary>
        /// The "EmptyTexture" property provides a static, single-pixel texture with a fully transparent color.
        /// This texture is commonly used as a placeholder or default visual element for user interfaces where
        /// no specific texture is required. The property ensures that the texture is initialized and cached
        /// to improve performance by reusing the same instance throughout the application lifecycle.
        /// </summary>
        public static Texture2D EmptyTexture
        {
            get
            {
                if (cachedEmptyTexture != null)
                    return cachedEmptyTexture;
                
                cachedEmptyTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                cachedEmptyTexture.SetPixel(0, 0, PRF.ExtraColor.Empty);
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

        /// <summary>
        /// Represents the base class for creating and managing GUI elements in the Autumn framework.
        /// Provides functionality such as animation handling, caching, localization, and layer management.
        /// </summary>
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

        /// <summary>
        /// Determines and assigns a unique layer for a GUI element instance.
        /// </summary>
        /// <param name="layerToSet">The layer to be assigned, if valid. If the provided layer is invalid or already used, a new unique layer is generated.</param>
        /// <param name="name">The name of the GUI element for which the layer is being assigned. Used for logging purposes in case of conflicts.</param>
        /// <returns>Returns the assigned unique layer for the GUI element.</returns>
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

        /// <summary>
        /// Executes logic when the GUI element is disabled.
        /// </summary>
        /// <remarks>
        /// This method is triggered when the GUI element transitions from an active to an inactive state.
        /// It is used for cleaning up, releasing resources, or performing necessary operations to properly
        /// deactivate the GUI element. The specific implementation details are defined in the deriving class
        /// to allow customized behavior for different GUI components.
        /// </remarks>
        protected virtual void OnDisable()
        {
        }

        /// <summary>
        /// Executes logic when the GUI element is enabled.
        /// </summary>
        /// <remarks>
        /// This method is invoked when the GUI element transitions from an inactive to an active state.
        /// It is responsible for initializing or preparing any necessary properties or operations required
        /// for the GUI element to function properly. The specific implementation is defined in the
        /// corresponding deriving class, allowing customized behavior for different GUI components.
        /// </remarks>
        protected virtual void OnEnable()
        {
        }

        /// <summary>
        /// Renders the graphical user interface (GUI) for the current element.
        /// </summary>
        /// <remarks>
        /// This method is responsible for rendering the visual components of the GUI element.
        /// The implementation details are defined in the deriving classes, ensuring that each specific GUI type
        /// handles its own rendering logic. If this method is invoked on a GUI element, it will execute the
        /// drawing logic associated with the specific GUI type's appearance and behavior.
        /// </remarks>
        protected internal abstract void Draw();

        /// <summary>
        /// Loads a texture asset from the provided GUIBase instance directory using the specified name and file extension.
        /// </summary>
        /// <param name="_base">The GUIBase instance from which the texture is to be loaded.</param>
        /// <param name="name">The name of the texture file to be loaded, excluding its extension.</param>
        /// <param name="ext">The extension of the texture file (e.g., "png"). Default is "png".</param>
        /// <returns>Returns the loaded Texture2D object if the file exists and is valid. Returns null if the file does not exist or loading fails.</returns>
        public static Texture2D LoadTexture(GUIBase _base, string name, string ext = "png")
        {
            return _base.LoadTexture(name, ext);
        }

        /// <summary>
        /// Clears all cached texture data stored in the GUI element's texture cache.
        /// </summary>
        /// <remarks>
        /// This method is used to free up memory by removing all entries from the texture cache dictionary.
        /// It is typically invoked to ensure that outdated or unnecessary textures are discarded, allowing
        /// for fresh loading of resources as needed.
        /// </remarks>
        public void ClearCache()
        {
            textureCache.Clear();
        }

        /// <summary>
        /// Disables the GUI element by triggering a closing animation and preparing it for deactivation.
        /// </summary>
        /// <remarks>
        /// This method prevents the GUI element from remaining active by initiating a close animation via the `GUIAnimation` instance.
        /// It ensures no further interaction with the element by setting the `OnGUI` callback to the animation closure. If the element
        /// is already inactive, this method returns immediately without performing any actions.
        /// </remarks>
        public void Disable()
        {
            if (!IsActive)
                return;
            OnGUI = animator.StartClose(DisableImmediate);
        }

        /// <summary>
        /// Immediately disables the GUI element and releases its associated resources.
        /// </summary>
        /// <remarks>
        /// This method ensures that the GUI element is fully disabled by updating its internal state,
        /// unloading localization resources, disabling the associated drawer, and invoking any required cleanup operations.
        /// It sets the element to an inactive state and clears the `OnGUI` callback. This method is called
        /// when the GUI element must transition to its disabled state without delay.
        /// </remarks>
        public void DisableImmediate()
        {
            if (!UIManager.Disable(this)) return;
            
            Drawer.Disable();
            OnDisable();
            locale.Unload();
            IsActive = false;
            OnGUI = null;
        }

        /// <summary>
        /// Activates the GUI element, preparing it for interaction and rendering.
        /// </summary>
        /// <remarks>
        /// This method ensures the GUI element is properly enabled by verifying its active state,
        /// invoking necessary initialization processes, loading related resources, and triggering animations.
        /// It also updates the internal state of the GUI element to reflect its active status.
        /// </remarks>
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

        /// <summary>
        /// Enables the GUI immediately, initializing necessary components and setting its state to active.
        /// </summary>
        /// <remarks>
        /// This method ensures that the GUI is activated instantly by loading its locale,
        /// invoking the relevant enable actions, and assigning the appropriate drawing function.
        /// </remarks>
        public void EnableImmediate()
        {
            // TODO seems to get stuck around here
            if (!UIManager.Enable(this)) return;
            
            locale.Load();
            OnEnable();
            IsActive = true;
            OnGUI = Draw;
        }

        /// <summary>
        /// Enables a specified GUI element after the current active GUI element completes its closing process and is disabled.
        /// </summary>
        /// <param name="next">The GUI element to be enabled after the current one is disabled.</param>
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

        /// <summary>
        /// Loads a texture from the specified directory for the GUI element.
        /// </summary>
        /// <param name="namebase">The base name of the texture to be loaded (excluding its extension).</param>
        /// <param name="ext">The file extension of the texture. If empty, the method expects the base name to already include a valid extension like "png", "jpg", or "jpeg".</param>
        /// <returns>Returns the loaded Texture2D object if successful, or a black texture in case of failure or invalid input.</returns>
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
            res.LoadRawTextureData(File.ReadAllBytes(path));
            //res.LoadImage(File.ReadAllBytes(path));
            res.Apply();
            textureCache.Add(namebase, res);
            return res;
        }

        /// <summary>
        /// Handles scaling updates for the GUIBase element.
        /// This method can be overridden to implement specific scaling logic for a GUI element
        /// and is typically called during size or resolution changes to ensure proper UI scaling.
        /// </summary>
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