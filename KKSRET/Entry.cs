using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using Fbx;
using TexFac;
using UnityEngine;

namespace KKSRET
{
    [BepInPlugin("org.fox.FBX", "FBX", "0.1")]
    public class Entry : BaseUnityPlugin
    {
        private class DeferredSMREntry
        {
            public SkinnedMeshRenderer smr;
            public long modelId;
    
            public DeferredSMREntry(SkinnedMeshRenderer smr, long modelId)
            {
                this.smr = smr;
                this.modelId = modelId;
            }
        }
        public static ConfigEntry<bool> useless;

        private void Awake()
        {
            useless = Config.Bind("General", "Useless", true);
            // TestExtract();
        }

        private static void TestExtract()
        {
            var exporter = new FbxExporter();
            
            exporter.AddSceneInfo(new FbxExporter.FbxSceneInfoData());
            

            var exported = new Dictionary<UnityEngine.Object, long>();

            var deferredSMRs = new List<DeferredSMREntry>();

            foreach (var objectCtrlInfo in KKAPI.Studio.StudioAPI.GetSelectedObjects())
            {
                var target = objectCtrlInfo.guideObject.transformTarget;
                ExportTransformRecursive(exporter, exported, deferredSMRs, target, null);
            }

            foreach (var entry in deferredSMRs)
            {
                ExportSkinAndBlendShapes(exporter, exported, entry.smr, entry.modelId);
            }

            var doc = exporter.GetDocument();
            FbxIO.WriteBinary(doc, "testfbx.fbx");
        }

        private static long ExportTransformRecursive(FbxExporter exporter,
            Dictionary<UnityEngine.Object, long> exported,
            List<DeferredSMREntry> deferredSMRs,
            Transform transform, long? parentId)
        {
            if (exported.TryGetValue(transform, out long existingId))
                return existingId;

            var meshFilter = transform.GetComponent<MeshFilter>();
            var meshRenderer = transform.GetComponent<MeshRenderer>();
            var skinnedRenderer = transform.GetComponent<SkinnedMeshRenderer>();

            bool hasMesh = (meshFilter != null && meshRenderer != null && meshFilter.sharedMesh != null)
                        || (skinnedRenderer != null && skinnedRenderer.sharedMesh != null);

            var modelData = FbxExporter.ExtractModel(transform, parentId, hasMesh ? "Mesh" : "Null");
            long modelId = exporter.AddModel(modelData);
            exported[transform] = modelId;

            if (meshFilter != null && meshRenderer != null && meshFilter.sharedMesh != null)
            {
                long geoId = ExportGeometry(exporter, exported, meshFilter.sharedMesh);
                exporter.ConnectGeometryToModel(geoId, modelId);
                ExportMaterials(exporter, exported, meshRenderer.sharedMaterials, modelId);
            }

            if (skinnedRenderer != null && skinnedRenderer.sharedMesh != null)
            {
                long geoId = ExportGeometry(exporter, exported, skinnedRenderer.sharedMesh);
                exporter.ConnectGeometryToModel(geoId, modelId);
                ExportMaterials(exporter, exported, skinnedRenderer.sharedMaterials, modelId);

                if (skinnedRenderer.bones != null)
                {
                    foreach (var bone in skinnedRenderer.bones)
                    {
                        if (bone != null)
                            EnsureBoneChain(exporter, exported, deferredSMRs, bone);
                    }
                }

                deferredSMRs.Add(new DeferredSMREntry(skinnedRenderer, modelId));
            }

            for (int i = 0; i < transform.childCount; i++)
                ExportTransformRecursive(exporter, exported, deferredSMRs, transform.GetChild(i), modelId);

            return modelId;
        }

        private static long EnsureBoneChain(FbxExporter exporter,
            Dictionary<UnityEngine.Object, long> exported,
            List<DeferredSMREntry> deferredSMRs,
            Transform bone)
        {
            if (exported.TryGetValue(bone, out long existingId))
                return existingId;

            long? parentId = null;
            if (bone.parent != null)
                parentId = EnsureBoneChain(exporter, exported, deferredSMRs, bone.parent);

            var modelData = FbxExporter.ExtractModel(bone, parentId, "LimbNode");
            long boneModelId = exporter.AddModel(modelData);
            exported[bone] = boneModelId;
            
            long nodeAttrId = exporter.AddNodeAttribute(bone.name, "LimbNode");
            exporter.ConnectNodeAttributeToModel(nodeAttrId, boneModelId);

            return boneModelId;
        }

        private static void ExportSkinAndBlendShapes(FbxExporter exporter,
            Dictionary<UnityEngine.Object, long> exported,
            SkinnedMeshRenderer smr, long modelId)
        {
            var mesh = smr.sharedMesh;
            long geoId = exported[mesh];

            var boneToId = new Dictionary<Transform, long>();
            if (smr.bones != null)
            {
                foreach (var bone in smr.bones)
                {
                    if (bone != null && exported.TryGetValue(bone, out long boneId))
                        boneToId[bone] = boneId;
                }
            }
            if (exported.TryGetValue(smr.transform, out long meshNodeId))
                boneToId[smr.transform] = meshNodeId;

            if (smr.bones != null && smr.bones.Length > 0 && mesh.boneWeights.Length > 0)
            {
                var skinData = FbxExporter.ExtractSkin(smr, boneToId);
                long skinId = exporter.AddSkin(skinData);
                exporter.ConnectSkinToGeometry(skinId, geoId);

                var poseData = FbxExporter.ExtractBindPose(smr, boneToId);
                poseData.source = new object();
                exporter.AddPose(poseData);
            }

            if (mesh.blendShapeCount > 0)
            {
                var bsData = FbxExporter.ExtractBlendShape(mesh);
                long bsId = exporter.AddBlendShape(bsData);
                exporter.ConnectBlendShapeToGeometry(bsId, geoId);
            }
        }

        private static long ExportGeometry(FbxExporter exporter,
            Dictionary<UnityEngine.Object, long> exported, Mesh mesh)
        {
            if (exported.TryGetValue(mesh, out long existingId))
                return existingId;

            long geoId = exporter.AddGeometry(FbxExporter.ExtractGeometry(mesh));
            exported[mesh] = geoId;
            return geoId;
        }

        private static void ExportMaterials(FbxExporter exporter,
            Dictionary<UnityEngine.Object, long> exported, Material[] materials, long modelId)
        {
            if (materials == null) return;

            foreach (var mat in materials)
            {
                if (mat == null) continue;

                long matId;
                if (exported.TryGetValue(mat, out long existingMatId))
                {
                    matId = existingMatId;
                }
                else
                {
                    matId = exporter.AddMaterial(FbxExporter.ExtractMaterial(mat));
                    exported[mat] = matId;
                    ExportMaterialTextures(exporter, exported, mat, matId);
                }

                exporter.ConnectMaterialToModel(matId, modelId);
            }
        }

        private static void ExportMaterialTextures(FbxExporter exporter,
            Dictionary<UnityEngine.Object, long> exported, Material mat, long matId)
        {
            TryExportTexture(exporter, exported, mat, matId, "_MainTex", "DiffuseColor");
            TryExportTexture(exporter, exported, mat, matId, "_BumpMap", "NormalMap");
            TryExportTexture(exporter, exported, mat, matId, "_EmissionMap", "EmissiveColor");
            TryExportTexture(exporter, exported, mat, matId, "_SpecGlossMap", "SpecularColor");
            TryExportTexture(exporter, exported, mat, matId, "_MetallicGlossMap", "SpecularColor");
            TryExportTexture(exporter, exported, mat, matId, "_OcclusionMap", "AmbientColor");
            TryExportTexture(exporter, exported, mat, matId, "_ParallaxMap", "Bump");
            TryExportTexture(exporter, exported, mat, matId, "_DetailAlbedoMap", "DiffuseColor");
            TryExportTexture(exporter, exported, mat, matId, "_DetailNormalMap", "NormalMap");

            TryExportTexture(exporter, exported, mat, matId, "_ColorMask", "DiffuseColor");
            TryExportTexture(exporter, exported, mat, matId, "_DetailMask", "DiffuseColor");
            TryExportTexture(exporter, exported, mat, matId, "_LineMask", "DiffuseColor");
            TryExportTexture(exporter, exported, mat, matId, "_AnotherRamp", "SpecularColor");
            TryExportTexture(exporter, exported, mat, matId, "_NormalMap", "NormalMap");
            TryExportTexture(exporter, exported, mat, matId, "_NormalMapDetail", "NormalMap");
        }

        private static void TryExportTexture(FbxExporter exporter,
            Dictionary<UnityEngine.Object, long> exported, Material mat, long matId,
            string unityProp, string fbxProp)
        {
            if (!mat.HasProperty(unityProp)) return;
            var tex = mat.GetTexture(unityProp);
            if (tex == null) return;

            if (exported.TryGetValue(tex, out long existingTexId))
            {
                exporter.ConnectTextureToMaterial(existingTexId, matId, fbxProp);
                return;
            }

            byte[] pngBytes = GetTextureBytes(tex);
            if (pngBytes == null) return;

            string texName = tex.name;
            if (string.IsNullOrEmpty(texName))
                texName = mat.name + "_" + unityProp.TrimStart('_');

            Vector2 scale = mat.GetTextureScale(unityProp);
            Vector2 offset = mat.GetTextureOffset(unityProp);

            var texData = new FbxExporter.FbxTextureData
            {
                source = tex,
                name = texName,
                filePath = texName + ".png",
                relativeFilePath = texName + ".png",
                uvSet = "UVChannel_1",
            };

            if (scale != Vector2.one) texData.scale = scale;
            if (offset != Vector2.zero) texData.translation = offset;

            var videoData = new FbxExporter.FbxVideoData
            {
                source = new object(),
                name = texName,
                filePath = texName + ".png",
                relativeFilePath = texName + ".png",
                content = pngBytes,
                width = tex.width,
                height = tex.height,
            };

            long texId = exporter.AddTexture(texData);
            long videoId = exporter.AddVideo(videoData);
            exported[tex] = texId;

            exporter.ConnectVideoToTexture(videoId, texId);
            exporter.ConnectTextureToMaterial(texId, matId, fbxProp);
        }

        private static byte[] GetTextureBytes(Texture tex)
        {
            if (tex == null) return null;

            if (tex is Texture2D tex2D && tex2D.isReadable)
            {
                try
                {
                    byte[] direct = tex2D.EncodeToPNG();
                    if (direct != null && direct.Length > 0)
                        return direct;
                }
                catch { }
            }

            RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(tex, rt);

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D readable = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            readable.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            byte[] bytes = readable.EncodeToPNG();
            Destroy(readable);
            return bytes;
        }
    }
}