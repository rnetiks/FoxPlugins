using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using ExtensibleSaveFormat;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using MessagePack;
using Studio;
using UnityEngine;

namespace KK
{
    public class SceneController : SceneCustomFunctionController
    {
        protected override void OnSceneLoad(
            SceneOperationKind operation,
            ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            CIO.binds.Clear();
            PluginData extendedData = GetExtendedData();
            object bytes;
            if (extendedData?.data == null || !extendedData.data.TryGetValue("binds", out bytes) || bytes == null)
                return;
            Entry.Logger.LogInfo($"Config: {((byte[]) bytes).Length} bytes");
            List<KeyValuePair<List<int>, int>> keyValuePairList = MessagePackSerializer.Deserialize<List<KeyValuePair<List<int>, int>>>((byte[]) bytes);
            Dictionary<KeyboardShortcut, List<ObjectCtrlInfo>> source = new Dictionary<KeyboardShortcut, List<ObjectCtrlInfo>>();
            foreach (KeyValuePair<List<int>, int> keyValuePair in keyValuePairList)
            {
                if (keyValuePair.Key.Count > 0)
                {
                    KeyboardShortcut key;
                    if (keyValuePair.Key.Count == 1)
                        key = new KeyboardShortcut((KeyCode) keyValuePair.Key[0]);
                    else if (keyValuePair.Key.Count > 1)
                    {
                        int[] numArray = new int[keyValuePair.Key.Count - 1];
                        Array.Copy(keyValuePair.Key.ToArray(), 1, numArray, 0, numArray.Length);
                        key = new KeyboardShortcut((KeyCode) keyValuePair.Key[0], Array.ConvertAll(numArray, e => (KeyCode) e));
                    }
                    if (source.ContainsKey(key))
                        source[key].Add(loadedItems[keyValuePair.Value]);
                    else
                        source.Add(key, new List<ObjectCtrlInfo>()
                        {
                            loadedItems[keyValuePair.Value]
                        });
                }
            }
            CIO.binds = source.ToDictionary<KeyValuePair<KeyboardShortcut, List<ObjectCtrlInfo>>, KeyboardShortcut, IEnumerable<ObjectCtrlInfo>>(kvp => kvp.Key,
                kvp => kvp.Value);
        }

        protected override void OnSceneSave()
        {
            PluginData data = new PluginData();
            List<KeyValuePair<List<int>, int>> keyValuePairList = new List<KeyValuePair<List<int>, int>>();
            foreach (KeyValuePair<int, ObjectCtrlInfo> keyValuePair in Singleton<Studio.Studio>.Instance.dicObjectCtrl)
            {
                foreach (KeyValuePair<KeyboardShortcut, IEnumerable<ObjectCtrlInfo>> bind in CIO.binds)
                {
                    foreach (ObjectCtrlInfo objectCtrlInfo in bind.Value)
                    {
                        if (objectCtrlInfo == keyValuePair.Value)
                        {
                            List<int> key = ConvertCodes(bind.Key.MainKey, bind.Key.Modifiers.ToArray());
                            keyValuePairList.Add(new KeyValuePair<List<int>, int>(key, keyValuePair.Key));
                        }
                    }
                }
            }
            byte[] array = MessagePackSerializer.Serialize(keyValuePairList);
            Entry.Logger.LogInfo($"{array.Length.ToString()} bytes <{string.Join(" ", Array.ConvertAll(array, input => input.ToString()))}>");
            data.data.Add("binds", array);
            SetExtendedData(data);
        }

        public List<int> ConvertCodes(KeyCode baseKey, params KeyCode[] mods)
        {
            List<KeyCode> keyCodeList = new List<KeyCode>
            {
                baseKey
            };
            keyCodeList.AddRange(mods);
            return Array.ConvertAll(keyCodeList.ToArray(), input => (int) input).ToList();
        }
    }
}