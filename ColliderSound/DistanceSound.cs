using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using KKAPI.Studio;
using KKAPI.Utilities;
using Studio;
using UnityEngine;

namespace ColliderSound.KK
{
    internal class DistanceSound : MonoBehaviour
    {
        class AudioContainer
        {
            public string Name;
            public ObjectCtrlInfo attractor;
            public ObjectCtrlInfo collider;
            public AudioSource _source;
            public bool killOnRemoval;
            public float requiredDistance;
            public float volume = 1f;

            public string SongName;
            public string AttractorName;
            public string ColliderName;
        }

        private List<AudioContainer> audios;
        private Rect _windowRect;
        private bool showMenu;

        private void Awake()
        {
            audios = new List<AudioContainer>();
            _windowRect = new Rect(200, 200, 500, 300);
        }

        private void DeleteCollider(int index)
        {
            Destroy(audios[index]._source);
            audios.RemoveAt(index);
        }

        private void OnGUI()
        {
            if (!showMenu)
                return;
            _windowRect = GUI.Window(8493, _windowRect, Func, "Collider Sound");
            if (_windowRect.Contains(Event.current.mousePosition))
            {
                Input.ResetInputAxes();
            }
        }

        private void Update()
        {
            if (Entry.toggleMenu.Value.IsDown())
            {
                showMenu = !showMenu;
            }
        }

        private Vector2 _scrollPosition;
        private int selectedIndex = -1;

        private void Func(int id)
        {
            var position = new Rect(0, 20, _windowRect.width - 205, 240);
            _scrollPosition = GUI.BeginScrollView(position, _scrollPosition, new Rect(0, 0, 180, 25 * audios.Count));
            int row = 0;
            foreach (var audioContainer in audios)
            {
                if (GUI.Button(new Rect(5, row, position.width - 25, 20), audioContainer.Name))
                {
                    selectedIndex = audios.IndexOf(audioContainer);
                }

                row += 25;
            }

            GUI.EndScrollView();
            if (GUI.Button(new Rect(5, _windowRect.height - 25, _windowRect.width - 210, 20), "Add New AudioCollider"))
            {
                var audioContainer = new AudioContainer()
                {
                    Name = "New AudioCollider",
                    _source = null,
                    attractor = null,
                    collider = null,
                    killOnRemoval = false,
                    requiredDistance = Entry.minDistance.Value
                };

                audios.Add(audioContainer);

                selectedIndex = audios.IndexOf(audioContainer);
            }

            if (selectedIndex < 0)
            {
                GUI.DragWindow();
                return;
            }

            // BUTTONS
            var container = audios[selectedIndex];
            if (GUI.Button(new Rect(_windowRect.width - 200, 20, 195, 20),
                    container._source == null
                        ? "Audio"
                        : "Audio: " + Path.GetFileNameWithoutExtension(container.SongName)))
            {
                var showDialog = OpenFileDialog.ShowDialog("Select Audio Source", "C:", "Audio Files (*.wav)|*.wav",
                    ".wav", OpenFileDialog.OpenSaveFileDialgueFlags.OFN_FILEMUSTEXIST);
                if (showDialog.Length > 0)
                {
                    var songPath = showDialog[0];
                    container.SongName = songPath;
                    container._source = gameObject.AddComponent<AudioSource>();
                    container._source.clip = LoadAudioFromPath(songPath);
                }
            }

            string colliderText = container.collider == null
                ? "Collider..."
                : $"Collider {container.ColliderName}";
            // TODO Change naming
            var objectCtrlInfos = StudioAPI.GetSelectedObjects();
            var ctrlInfos = objectCtrlInfos as ObjectCtrlInfo[] ?? objectCtrlInfos.ToArray();
            if (GUI.Button(new Rect(_windowRect.width - 200, 45, 195, 20), colliderText))
            {
                var b = ctrlInfos.First() is OCIItem;
                if (ctrlInfos.Any() && b)
                {
                    var objectCtrlInfo = ctrlInfos.First();
                    container.ColliderName = objectCtrlInfo.treeNodeObject.textName;
                    container.collider = objectCtrlInfo;
                }
            }

            string attractorText = container.attractor == null
                ? "Attractor..."
                : $"Attractor {container.AttractorName}";
            if (GUI.Button(new Rect(_windowRect.width - 200, 70, 195, 20), attractorText))
            {
                var b = ctrlInfos.First() is OCIItem;
                if (ctrlInfos.Any() && b)
                {
                    var objectCtrlInfo = ctrlInfos.First();
                    container.AttractorName = objectCtrlInfo.treeNodeObject.textName;
                    container.attractor = objectCtrlInfo;
                }
            }


            container.Name = GUI.TextField(new Rect(_windowRect.width - 200, 95, 195, 20),
                container.Name);

            if (GUI.Button(new Rect(_windowRect.width - 200, 120, 195, 20),
                    "Kill on Removal: " + container.killOnRemoval))
            {
                container.killOnRemoval = !container.killOnRemoval;
            }

            container.volume = Mathf.Clamp01(float.Parse(GUI.TextField(new Rect(_windowRect.width - 200, 145, 195, 20),
                "Volume: " + container.volume.ToString(CultureInfo.InvariantCulture)).Replace("Volume: ", "")));

            container.requiredDistance = Mathf.Abs(float.Parse(GUI
                .TextField(new Rect(_windowRect.width - 200, 170, 140, 20),
                    "Distance: " + container.requiredDistance.ToString(CultureInfo.InvariantCulture)).Substring(10)));
            if (GUI.Button(new Rect(_windowRect.width - 55, 170, 50, 20), "Auto"))
            {
                if (container.attractor != null && container.collider != null)
                {
                    container.requiredDistance = Vector3.Distance(container.attractor.guideObject.changeAmount.pos,
                        container.collider.guideObject.changeAmount.pos) * 10;
                }
            }


            if (GUI.Button(new Rect(_windowRect.width - 200, _windowRect.height - 25, 195, 20), "Delete AudioCollider"))
            {
                DeleteCollider(selectedIndex);
                if (audios.Count > 0)
                    selectedIndex = audios.Count - 1;
                else
                    selectedIndex = -1;
            }

            GUI.DragWindow();
        }

        private void FixedUpdate()
        {
            PlayAudios();
        }

        private Bounds GetBounds(ObjectCtrlInfo item)
        {
            var bounds = new Bounds();
            var guideObjectTransformTarget = item.guideObject.transformTarget;
            var mr = guideObjectTransformTarget.GetComponentsInChildren<MeshRenderer>();
            var smr = guideObjectTransformTarget.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (var meshRenderer in mr)
            {
                var meshRendererBounds = meshRenderer.bounds;
                bounds.max = new Vector3(
                    Mathf.Max(bounds.max.x, meshRendererBounds.max.x),
                    Mathf.Max(bounds.max.y, meshRendererBounds.max.y),
                    Mathf.Max(bounds.max.z, meshRendererBounds.max.z)
                );
            }

            foreach (var meshRenderer in smr)
            {
                var meshRendererBounds = meshRenderer.bounds;
                bounds.max = new Vector3(
                    Mathf.Max(bounds.max.x, meshRendererBounds.max.x),
                    Mathf.Max(bounds.max.y, meshRendererBounds.max.y),
                    Mathf.Max(bounds.max.z, meshRendererBounds.max.z)
                );
            }

            return bounds;
        }

        private void PlayAudios()
        {
            foreach (var src in audios)
            {
                if (src._source == null || src.attractor == null || src.collider == null)
                    continue;
                // var intersects = src.attractor.Intersects(src.collider);
                var boundsCollider = GetBounds(src.collider);
                var boundsAttractor = GetBounds(src.attractor);
                if (boundsAttractor == new Bounds() || boundsCollider == new Bounds())
                    continue;
                var intersects =
                    Vector3.Distance(src.collider.guideObject.changeAmount.pos,
                        src.attractor.guideObject.changeAmount.pos) <
                    Mathf.Max(src.requiredDistance, Entry.minDistance.Value) / 10;
                switch (intersects)
                {
                    case true when !src._source.isPlaying:
                        src._source.volume = src.volume;
                        src._source.Play();
                        break;
                    case false when src._source.isPlaying:
                        if (src.killOnRemoval)
                        {
                            src._source.Stop();
                        }

                        break;
                }
            }
        }

        private static AudioClip LoadAudioFromPath(string path)
        {
            if (Path.GetExtension(path).ToLower() == ".wav")
            {
                byte[] fileData = File.ReadAllBytes(path);
                Wav wav = new Wav(fileData);
                Entry._logger.LogWarning($"WAV File: {fileData.Length}, {wav.SampleCount}, {wav.Channels}, {wav.Frequency}");
                AudioClip audioClip = AudioClip.Create(Path.GetFileNameWithoutExtension(path), wav.SampleCount,
                    wav.Channels, wav.Frequency, false);
                audioClip.SetData(wav.LeftChannel, 0);
                return audioClip;
            }

            Debug.LogError("Unsupported audio format: " + path);
            return null;
        }
    }
}