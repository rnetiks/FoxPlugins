using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Fbx;

public class FbxExporter
{
    private FbxDocument doc;
    private Dictionary<object, long> objectIds = new Dictionary<object, long>();
    private List<FbxConnection> connections = new List<FbxConnection>();
    private long nextId = 100000;

    public FbxExporter()
    {
        doc = new FbxDocument() { Version = FbxVersion.v7_4 };
        InitializeDocument();
    }

    #region Public API - Scene Objects

    public long AddModel(FbxModelData modelData)
    {
        long id = GetOrCreateId(modelData.source);

        var model = new FbxNode()
        {
            Name = "Model",
            Properties = { id, modelData.name + "\x00\x01Model", modelData.modelType }
        };

        model.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 232 } });

        var props = CreateProperties70();

        AddProperty(props, "Lcl Translation", "Lcl Translation", "", "A",
            (double)modelData.localPosition.x,
            (double)modelData.localPosition.y,
            (double)modelData.localPosition.z);

        AddProperty(props, "Lcl Rotation", "Lcl Rotation", "", "A",
            (double)modelData.localRotation.x,
            (double)modelData.localRotation.y,
            (double)modelData.localRotation.z);

        AddProperty(props, "Lcl Scaling", "Lcl Scaling", "", "A",
            (double)modelData.localScale.x,
            (double)modelData.localScale.y,
            (double)modelData.localScale.z);

        if (modelData.preRotation != null)
        {
            var pre = modelData.preRotation.Value;
            AddProperty(props, "PreRotation", "Vector3D", "Vector", "",
                (double)pre.x, (double)pre.y, (double)pre.z);
        }

        if (modelData.postRotation != null)
        {
            var post = modelData.postRotation.Value;
            AddProperty(props, "PostRotation", "Vector3D", "Vector", "",
                (double)post.x, (double)post.y, (double)post.z);
        }

        if (modelData.rotationOffset != null)
        {
            var ro = modelData.rotationOffset.Value;
            AddProperty(props, "RotationOffset", "Vector3D", "Vector", "",
                (double)ro.x, (double)ro.y, (double)ro.z);
        }

        if (modelData.rotationPivot != null)
        {
            var rp = modelData.rotationPivot.Value;
            AddProperty(props, "RotationPivot", "Vector3D", "Vector", "",
                (double)rp.x, (double)rp.y, (double)rp.z);
        }

        if (modelData.scalingOffset != null)
        {
            var so = modelData.scalingOffset.Value;
            AddProperty(props, "ScalingOffset", "Vector3D", "Vector", "",
                (double)so.x, (double)so.y, (double)so.z);
        }

        if (modelData.scalingPivot != null)
        {
            var sp = modelData.scalingPivot.Value;
            AddProperty(props, "ScalingPivot", "Vector3D", "Vector", "",
                (double)sp.x, (double)sp.y, (double)sp.z);
        }

        if (modelData.rotationOrder.HasValue)
            AddProperty(props, "RotationOrder", "enum", "", "", (int)modelData.rotationOrder.Value);

        if (modelData.visibility != null)
            AddProperty(props, "Visibility", "Visibility", "", "A", modelData.visibility.Value ? 1.0 : 0.0);

        if (modelData.visibilityInheritance.HasValue)
            AddProperty(props, "Visibility Inheritance", "Visibility Inheritance", "", "",
                modelData.visibilityInheritance.Value ? 1 : 0);

        if (modelData.show.HasValue)
            AddProperty(props, "Show", "bool", "", "", modelData.show.Value ? 1 : 0);

        if (modelData.freeze.HasValue)
            AddProperty(props, "Freeze", "bool", "", "", modelData.freeze.Value ? 1 : 0);

        if (modelData.lodBox.HasValue)
            AddProperty(props, "LODBox", "bool", "", "", modelData.lodBox.Value ? 1 : 0);

        AddProperty(props, "DefaultAttributeIndex", "int", "Integer", "", 0);
        AddProperty(props, "InheritType", "enum", "", "", 1);

        if (modelData.customProperties != null)
        {
            foreach (var prop in modelData.customProperties)
            {
                AddProperty(props, prop.Key, prop.Value.type, prop.Value.subType, prop.Value.flags, prop.Value.values);
            }
        }

        model.Nodes.Add(props);

        model.Nodes.Add(new FbxNode() { Name = "MultiLayer", Properties = { 0 } });
        model.Nodes.Add(new FbxNode() { Name = "MultiTake", Properties = { 0 } });

        model.Nodes.Add(new FbxNode() { Name = "Shading", Properties = { modelData.shading } });

        model.Nodes.Add(new FbxNode() { Name = "Culling", Properties = { modelData.culling } });

        GetObjectsNode().Nodes.Add(model);

        if (modelData.parentId.HasValue)
            connections.Add(new FbxConnection("OO", id, modelData.parentId.Value));
        else
            connections.Add(new FbxConnection("OO", id, 0));

        return id;
    }
    public long AddGeometry(FbxGeometryData geometryData)
    {
        long id = GetOrCreateId(geometryData.source);

        var geometry = new FbxNode()
        {
            Name = "Geometry",
            Properties = { id, geometryData.name + "\x00\x01Geometry", "Mesh" }
        };

        geometry.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 232 } });

        var vertices = new double[geometryData.vertices.Length * 3];
        for (int i = 0; i < geometryData.vertices.Length; i++)
        {
            Vector3 v = geometryData.vertices[i];
            vertices[i * 3 + 0] = v.x;
            vertices[i * 3 + 1] = v.y;
            vertices[i * 3 + 2] = v.z;
        }
        geometry.Nodes.Add(new FbxNode() { Name = "Vertices", Properties = { vertices } });

        var indices = new int[geometryData.triangles.Length];
        for (int i = 0; i < geometryData.triangles.Length; i += 3)
        {
            indices[i + 0] = geometryData.triangles[i + 0];
            indices[i + 1] = geometryData.triangles[i + 1];
            indices[i + 2] = -(geometryData.triangles[i + 2] + 1);
        }
        geometry.Nodes.Add(new FbxNode() { Name = "PolygonVertexIndex", Properties = { indices } });

        if (geometryData.edges != null && geometryData.edges.Length > 0)
        {
            geometry.Nodes.Add(new FbxNode() { Name = "Edges", Properties = { geometryData.edges } });
        }

        geometry.Nodes.Add(new FbxNode() { Name = "GeometryVersion", Properties = { 124 } });

        var layer = new FbxNode() { Name = "Layer", Properties = { 0 } };
        layer.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 100 } });

        int layerElementIndex = 0;

        if (geometryData.normals != null && geometryData.normals.Length > 0)
        {
            AddLayerElementNormal(geometry, geometryData.normals, geometryData.triangles, 0);
            var layerElementNormal = new FbxNode() { Name = "LayerElement" };
            layerElementNormal.Nodes.Add(new FbxNode() { Name = "Type", Properties = { "LayerElementNormal" } });
            layerElementNormal.Nodes.Add(new FbxNode() { Name = "TypedIndex", Properties = { 0 } });
            layer.Nodes.Add(layerElementNormal);
        }

        if (geometryData.binormals != null && geometryData.binormals.Length > 0)
        {
            AddLayerElementBinormal(geometry, geometryData.binormals, geometryData.triangles, 0);
            var layerElementBinormal = new FbxNode() { Name = "LayerElement" };
            layerElementBinormal.Nodes.Add(new FbxNode() { Name = "Type", Properties = { "LayerElementBinormal" } });
            layerElementBinormal.Nodes.Add(new FbxNode() { Name = "TypedIndex", Properties = { 0 } });
            layer.Nodes.Add(layerElementBinormal);
        }

        if (geometryData.tangents != null && geometryData.tangents.Length > 0)
        {
            AddLayerElementTangent(geometry, geometryData.tangents, geometryData.triangles, 0);
            var layerElementTangent = new FbxNode() { Name = "LayerElement" };
            layerElementTangent.Nodes.Add(new FbxNode() { Name = "Type", Properties = { "LayerElementTangent" } });
            layerElementTangent.Nodes.Add(new FbxNode() { Name = "TypedIndex", Properties = { 0 } });
            layer.Nodes.Add(layerElementTangent);
        }

        if (geometryData.uvChannels != null)
        {
            for (int i = 0; i < geometryData.uvChannels.Length; i++)
            {
                if (geometryData.uvChannels[i] != null && geometryData.uvChannels[i].Length > 0)
                {
                    AddLayerElementUV(geometry, geometryData.uvChannels[i], geometryData.triangles, i);
                    var layerElementUV = new FbxNode() { Name = "LayerElement" };
                    layerElementUV.Nodes.Add(new FbxNode() { Name = "Type", Properties = { "LayerElementUV" } });
                    layerElementUV.Nodes.Add(new FbxNode() { Name = "TypedIndex", Properties = { i } });
                    layer.Nodes.Add(layerElementUV);
                }
            }
        }

        if (geometryData.colorChannels != null)
        {
            for (int i = 0; i < geometryData.colorChannels.Length; i++)
            {
                if (geometryData.colorChannels[i] != null && geometryData.colorChannels[i].Length > 0)
                {
                    AddLayerElementColor(geometry, geometryData.colorChannels[i], geometryData.triangles, i);
                    var layerElementColor = new FbxNode() { Name = "LayerElement" };
                    layerElementColor.Nodes.Add(new FbxNode() { Name = "Type", Properties = { "LayerElementColor" } });
                    layerElementColor.Nodes.Add(new FbxNode() { Name = "TypedIndex", Properties = { i } });
                    layer.Nodes.Add(layerElementColor);
                }
            }
        }

        if (geometryData.materialIndices != null && geometryData.materialIndices.Length > 0)
        {
            AddLayerElementMaterial(geometry, geometryData.materialIndices);
            var layerElementMaterial = new FbxNode() { Name = "LayerElement" };
            layerElementMaterial.Nodes.Add(new FbxNode() { Name = "Type", Properties = { "LayerElementMaterial" } });
            layerElementMaterial.Nodes.Add(new FbxNode() { Name = "TypedIndex", Properties = { 0 } });
            layer.Nodes.Add(layerElementMaterial);
        }

        if (geometryData.smoothing != null && geometryData.smoothing.Length > 0)
        {
            AddLayerElementSmoothing(geometry, geometryData.smoothing);
            var layerElementSmoothing = new FbxNode() { Name = "LayerElement" };
            layerElementSmoothing.Nodes.Add(new FbxNode() { Name = "Type", Properties = { "LayerElementSmoothing" } });
            layerElementSmoothing.Nodes.Add(new FbxNode() { Name = "TypedIndex", Properties = { 0 } });
            layer.Nodes.Add(layerElementSmoothing);
        }

        if (geometryData.edgeCrease != null && geometryData.edgeCrease.Length > 0)
        {
            AddLayerElementCrease(geometry, geometryData.edgeCrease, "EdgeCrease");
            var layerElementCrease = new FbxNode() { Name = "LayerElement" };
            layerElementCrease.Nodes.Add(new FbxNode() { Name = "Type", Properties = { "LayerElementCrease" } });
            layerElementCrease.Nodes.Add(new FbxNode() { Name = "TypedIndex", Properties = { 0 } });
            layer.Nodes.Add(layerElementCrease);
        }

        if (geometryData.vertexCrease != null && geometryData.vertexCrease.Length > 0)
        {
            AddLayerElementCrease(geometry, geometryData.vertexCrease, "VertexCrease");
            var layerElementCrease = new FbxNode() { Name = "LayerElement" };
            layerElementCrease.Nodes.Add(new FbxNode() { Name = "Type", Properties = { "LayerElementCrease" } });
            layerElementCrease.Nodes.Add(new FbxNode() { Name = "TypedIndex", Properties = { 1 } });
            layer.Nodes.Add(layerElementCrease);
        }

        if (geometryData.holes != null && geometryData.holes.Length > 0)
        {
            AddLayerElementHole(geometry, geometryData.holes);
            var layerElementHole = new FbxNode() { Name = "LayerElement" };
            layerElementHole.Nodes.Add(new FbxNode() { Name = "Type", Properties = { "LayerElementHole" } });
            layerElementHole.Nodes.Add(new FbxNode() { Name = "TypedIndex", Properties = { 0 } });
            layer.Nodes.Add(layerElementHole);
        }

        if (geometryData.userDataLayers != null)
        {
            for (int i = 0; i < geometryData.userDataLayers.Length; i++)
            {
                AddLayerElementUserData(geometry, geometryData.userDataLayers[i], i);
                var layerElementUserData = new FbxNode() { Name = "LayerElement" };
                layerElementUserData.Nodes.Add(new FbxNode() { Name = "Type", Properties = { "LayerElementUserData" } });
                layerElementUserData.Nodes.Add(new FbxNode() { Name = "TypedIndex", Properties = { i } });
                layer.Nodes.Add(layerElementUserData);
            }
        }

        geometry.Nodes.Add(layer);

        GetObjectsNode().Nodes.Add(geometry);

        return id;
    }

    public long AddNurbsCurve(FbxNurbsCurveData curveData)
    {
        long id = GetOrCreateId(curveData.source);

        var nurbs = new FbxNode()
        {
            Name = "Geometry",
            Properties = { id, curveData.name + "\x00\x01Geometry", "NurbsCurve" }
        };

        nurbs.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 100 } });
        nurbs.Nodes.Add(new FbxNode() { Name = "Order", Properties = { curveData.order } });
        nurbs.Nodes.Add(new FbxNode() { Name = "Dimension", Properties = { curveData.dimension } });
        nurbs.Nodes.Add(new FbxNode() { Name = "Form", Properties = { curveData.isClosed ? "Closed" : "Open" } });
        nurbs.Nodes.Add(new FbxNode() { Name = "Rational", Properties = { curveData.isRational ? 1 : 0 } });

        var points = new double[curveData.controlPoints.Length * 4];
        for (int i = 0; i < curveData.controlPoints.Length; i++)
        {
            var p = curveData.controlPoints[i];
            points[i * 4 + 0] = p.x;
            points[i * 4 + 1] = p.y;
            points[i * 4 + 2] = p.z;
            points[i * 4 + 3] = curveData.weights != null ? curveData.weights[i] : 1.0;
        }
        nurbs.Nodes.Add(new FbxNode() { Name = "Points", Properties = { points } });

        nurbs.Nodes.Add(new FbxNode() { Name = "KnotVector", Properties = { curveData.knots } });

        GetObjectsNode().Nodes.Add(nurbs);

        return id;
    }

    public long AddNurbsSurface(FbxNurbsSurfaceData surfaceData)
    {
        long id = GetOrCreateId(surfaceData.source);

        var nurbs = new FbxNode()
        {
            Name = "Geometry",
            Properties = { id, surfaceData.name + "\x00\x01Geometry", "NurbsSurface" }
        };

        nurbs.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 100 } });
        nurbs.Nodes.Add(new FbxNode() { Name = "OrderU", Properties = { surfaceData.orderU } });
        nurbs.Nodes.Add(new FbxNode() { Name = "OrderV", Properties = { surfaceData.orderV } });
        nurbs.Nodes.Add(new FbxNode() { Name = "Dimension", Properties = { 3 } });
        nurbs.Nodes.Add(new FbxNode() { Name = "FormU", Properties = { surfaceData.isClosedU ? "Closed" : "Open" } });
        nurbs.Nodes.Add(new FbxNode() { Name = "FormV", Properties = { surfaceData.isClosedV ? "Closed" : "Open" } });
        nurbs.Nodes.Add(new FbxNode() { Name = "Rational", Properties = { surfaceData.isRational ? 1 : 0 } });

        var points = new double[surfaceData.controlPoints.Length * 4];
        for (int i = 0; i < surfaceData.controlPoints.Length; i++)
        {
            var p = surfaceData.controlPoints[i];
            points[i * 4 + 0] = p.x;
            points[i * 4 + 1] = p.y;
            points[i * 4 + 2] = p.z;
            points[i * 4 + 3] = surfaceData.weights != null ? surfaceData.weights[i] : 1.0;
        }
        nurbs.Nodes.Add(new FbxNode() { Name = "Points", Properties = { points } });

        nurbs.Nodes.Add(new FbxNode() { Name = "KnotVectorU", Properties = { surfaceData.knotsU } });
        nurbs.Nodes.Add(new FbxNode() { Name = "KnotVectorV", Properties = { surfaceData.knotsV } });

        GetObjectsNode().Nodes.Add(nurbs);

        return id;
    }

    public long AddMaterial(FbxMaterialData materialData)
    {
        long id = GetOrCreateId(materialData.source);

        var material = new FbxNode()
        {
            Name = "Material",
            Properties = { id, materialData.name + "\x00\x01Material", "" }
        };

        material.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 102 } });
        material.Nodes.Add(new FbxNode() { Name = "ShadingModel", Properties = { materialData.shadingModel } });
        material.Nodes.Add(new FbxNode() { Name = "MultiLayer", Properties = { 0 } });

        var props = CreateProperties70();

        if (materialData.diffuseColor.HasValue)
        {
            Color c = materialData.diffuseColor.Value;
            AddProperty(props, "DiffuseColor", "Color", "", "A", c.r, c.g, c.b);
        }

        if (materialData.diffuseFactor.HasValue)
            AddProperty(props, "DiffuseFactor", "Number", "", "A", materialData.diffuseFactor.Value);

        if (materialData.specularColor.HasValue)
        {
            Color c = materialData.specularColor.Value;
            AddProperty(props, "SpecularColor", "Color", "", "A", c.r, c.g, c.b);
        }

        if (materialData.specularFactor.HasValue)
            AddProperty(props, "SpecularFactor", "Number", "", "A", materialData.specularFactor.Value);

        if (materialData.emissiveColor.HasValue)
        {
            Color c = materialData.emissiveColor.Value;
            AddProperty(props, "EmissiveColor", "Color", "", "A", c.r, c.g, c.b);
        }

        if (materialData.emissiveFactor.HasValue)
            AddProperty(props, "EmissiveFactor", "Number", "", "A", materialData.emissiveFactor.Value);

        if (materialData.ambientColor.HasValue)
        {
            Color c = materialData.ambientColor.Value;
            AddProperty(props, "AmbientColor", "Color", "", "A", c.r, c.g, c.b);
        }

        if (materialData.ambientFactor.HasValue)
            AddProperty(props, "AmbientFactor", "Number", "", "A", materialData.ambientFactor.Value);

        if (materialData.transparentColor.HasValue)
        {
            Color c = materialData.transparentColor.Value;
            AddProperty(props, "TransparentColor", "Color", "", "A", c.r, c.g, c.b);
        }

        if (materialData.transparencyFactor.HasValue)
            AddProperty(props, "TransparencyFactor", "Number", "", "A", materialData.transparencyFactor.Value);

        if (materialData.opacity.HasValue)
            AddProperty(props, "Opacity", "double", "Number", "", materialData.opacity.Value);

        if (materialData.shininess.HasValue)
            AddProperty(props, "Shininess", "double", "Number", "", materialData.shininess.Value);

        if (materialData.reflectivity.HasValue)
            AddProperty(props, "Reflectivity", "double", "Number", "", materialData.reflectivity.Value);

        if (materialData.reflectionColor.HasValue)
        {
            Color c = materialData.reflectionColor.Value;
            AddProperty(props, "ReflectionColor", "Color", "", "A", c.r, c.g, c.b);
        }

        if (materialData.reflectionFactor.HasValue)
            AddProperty(props, "ReflectionFactor", "Number", "", "A", materialData.reflectionFactor.Value);

        if (materialData.metallic.HasValue)
            AddProperty(props, "Metallic", "Number", "", "A", materialData.metallic.Value);

        if (materialData.roughness.HasValue)
            AddProperty(props, "Roughness", "Number", "", "A", materialData.roughness.Value);

        if (materialData.normalScale.HasValue)
            AddProperty(props, "NormalScale", "Number", "", "A", materialData.normalScale.Value);

        if (materialData.bumpFactor.HasValue)
            AddProperty(props, "BumpFactor", "double", "Number", "", materialData.bumpFactor.Value);

        if (materialData.displacementFactor.HasValue)
            AddProperty(props, "DisplacementFactor", "double", "Number", "", materialData.displacementFactor.Value);

        if (materialData.customProperties != null)
        {
            foreach (var prop in materialData.customProperties)
            {
                AddProperty(props, prop.Key, prop.Value.type, prop.Value.subType, prop.Value.flags, prop.Value.values);
            }
        }

        material.Nodes.Add(props);

        GetObjectsNode().Nodes.Add(material);

        return id;
    }

    public long AddTexture(FbxTextureData textureData)
    {
        long id = GetOrCreateId(textureData.source);

        var texture = new FbxNode()
        {
            Name = "Texture",
            Properties = { id, textureData.name + "\x00\x01Texture", "" }
        };

        texture.Nodes.Add(new FbxNode() { Name = "Type", Properties = { "TextureVideoClip" } });
        texture.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 202 } });
        texture.Nodes.Add(new FbxNode() { Name = "TextureName", Properties = { textureData.name + "\x00\x01Texture" } });

        var props = CreateProperties70();

        AddProperty(props, "UVSet", "KString", "", "", textureData.uvSet ?? "UVChannel_1");
        AddProperty(props, "UseMaterial", "bool", "", "", 1);

        if (textureData.translation.HasValue)
        {
            var t = textureData.translation.Value;
            AddProperty(props, "Translation", "Vector", "", "A", t.x, t.y, 0.0);
        }

        if (textureData.scale.HasValue)
        {
            var s = textureData.scale.Value;
            AddProperty(props, "Scaling", "Vector", "", "A", s.x, s.y, 1.0);
        }

        if (textureData.rotation.HasValue)
        {
            AddProperty(props, "Rotation", "Vector", "", "A", 0.0, 0.0, textureData.rotation.Value);
        }

        if (textureData.wrapModeU.HasValue)
            AddProperty(props, "WrapModeU", "enum", "", "", (int)textureData.wrapModeU.Value);

        if (textureData.wrapModeV.HasValue)
            AddProperty(props, "WrapModeV", "enum", "", "", (int)textureData.wrapModeV.Value);

        if (textureData.blendMode.HasValue)
            AddProperty(props, "BlendMode", "enum", "", "", (int)textureData.blendMode.Value);

        if (textureData.alphaSource.HasValue)
            AddProperty(props, "Texture_Alpha_Source", "enum", "", "", (int)textureData.alphaSource.Value);

        if (textureData.cropping.HasValue)
        {
            var c = textureData.cropping.Value;
            AddProperty(props, "Cropping", "Vector4D", "Vector", "", c.x, c.y, c.z, c.w);
        }

        texture.Nodes.Add(props);

        texture.Nodes.Add(new FbxNode() { Name = "Media", Properties = { textureData.name + "\x00\x01Video" } });
        texture.Nodes.Add(new FbxNode() { Name = "FileName", Properties = { textureData.filePath } });
        texture.Nodes.Add(new FbxNode() { Name = "RelativeFilename", Properties = { textureData.relativeFilePath ?? textureData.filePath } });

        var modelUVTranslation = new FbxNode() { Name = "ModelUVTranslation", Properties = { 0.0, 0.0 } };
        var modelUVScaling = new FbxNode() { Name = "ModelUVScaling", Properties = { 1.0, 1.0 } };
        texture.Nodes.Add(modelUVTranslation);
        texture.Nodes.Add(modelUVScaling);

        texture.Nodes.Add(new FbxNode() { Name = "Texture_Alpha_Source", Properties = { "None" } });

        GetObjectsNode().Nodes.Add(texture);

        return id;
    }

    public long AddLayeredTexture(FbxLayeredTextureData layeredTextureData)
    {
        long id = GetOrCreateId(layeredTextureData.source);

        var layeredTexture = new FbxNode()
        {
            Name = "LayeredTexture",
            Properties = { id, layeredTextureData.name + "\x00\x01LayeredTexture", "" }
        };

        layeredTexture.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 100 } });

        var props = CreateProperties70();
        AddProperty(props, "BlendMode", "enum", "", "", (int)layeredTextureData.blendMode);
        AddProperty(props, "Alpha", "Number", "", "A", layeredTextureData.alpha);
        layeredTexture.Nodes.Add(props);

        GetObjectsNode().Nodes.Add(layeredTexture);

        return id;
    }

    public long AddVideo(FbxVideoData videoData)
    {
        long id = GetOrCreateId(videoData.source);

        var video = new FbxNode()
        {
            Name = "Video",
            Properties = { id, videoData.name + "\x00\x01Video", "Clip" }
        };

        video.Nodes.Add(new FbxNode() { Name = "Type", Properties = { "Clip" } });

        var props = CreateProperties70();
        AddProperty(props, "Path", "KString", "XRefUrl", "", videoData.filePath);

        if (videoData.width.HasValue && videoData.height.HasValue)
        {
            AddProperty(props, "Width", "int", "Integer", "", videoData.width.Value);
            AddProperty(props, "Height", "int", "Integer", "", videoData.height.Value);
        }

        video.Nodes.Add(props);

        video.Nodes.Add(new FbxNode() { Name = "UseMipMap", Properties = { 0 } });
        video.Nodes.Add(new FbxNode() { Name = "Filename", Properties = { videoData.filePath } });
        video.Nodes.Add(new FbxNode() { Name = "RelativeFilename", Properties = { videoData.relativeFilePath ?? videoData.filePath } });

        if (videoData.content != null && videoData.content.Length > 0)
        {
            video.Nodes.Add(new FbxNode() { Name = "Content", Properties = { videoData.content } });
        }

        GetObjectsNode().Nodes.Add(video);

        return id;
    }

    public long AddCamera(FbxCameraData cameraData)
    {
        long id = GetOrCreateId(cameraData.source);

        var nodeAttr = new FbxNode()
        {
            Name = "NodeAttribute",
            Properties = { id, cameraData.name + "\x00\x01NodeAttribute", "Camera" }
        };

        nodeAttr.Nodes.Add(new FbxNode() { Name = "TypeFlags", Properties = { "Camera" } });

        var props = CreateProperties70();

        AddProperty(props, "Position", "Vector", "", "A",
            cameraData.position.x, cameraData.position.y, cameraData.position.z);
        AddProperty(props, "UpVector", "Vector", "", "A",
            cameraData.upVector.x, cameraData.upVector.y, cameraData.upVector.z);
        AddProperty(props, "InterestPosition", "Vector", "", "A",
            cameraData.targetPosition.x, cameraData.targetPosition.y, cameraData.targetPosition.z);

        AddProperty(props, "FieldOfView", "FieldOfView", "", "A", cameraData.fieldOfView);
        AddProperty(props, "FieldOfViewX", "FieldOfView", "", "A", cameraData.fieldOfView);
        AddProperty(props, "FieldOfViewY", "FieldOfView", "", "A", cameraData.fieldOfView);
        AddProperty(props, "FocalLength", "Number", "", "A", cameraData.focalLength);

        AddProperty(props, "NearPlane", "double", "Number", "", cameraData.nearClipPlane);
        AddProperty(props, "FarPlane", "double", "Number", "", cameraData.farClipPlane);

        if (cameraData.isOrthographic)
        {
            AddProperty(props, "CameraProjectionType", "enum", "", "", 1);
            AddProperty(props, "OrthoZoom", "double", "Number", "", cameraData.orthographicSize);
        }
        else
        {
            AddProperty(props, "CameraProjectionType", "enum", "", "", 0);
        }

        AddProperty(props, "AspectWidth", "double", "Number", "", cameraData.aspectRatio);
        AddProperty(props, "AspectHeight", "double", "Number", "", 1.0);

        if (cameraData.backgroundColor.HasValue)
        {
            Color bg = cameraData.backgroundColor.Value;
            AddProperty(props, "BackgroundColor", "Color", "", "A", bg.r, bg.g, bg.b);
        }

        if (cameraData.filmWidth.HasValue)
            AddProperty(props, "FilmWidth", "double", "Number", "", cameraData.filmWidth.Value);

        if (cameraData.filmHeight.HasValue)
            AddProperty(props, "FilmHeight", "double", "Number", "", cameraData.filmHeight.Value);

        if (cameraData.filmAspectRatio.HasValue)
            AddProperty(props, "FilmAspectRatio", "double", "Number", "", cameraData.filmAspectRatio.Value);

        if (cameraData.apertureMode.HasValue)
            AddProperty(props, "ApertureMode", "enum", "", "", (int)cameraData.apertureMode.Value);

        if (cameraData.gateFit.HasValue)
            AddProperty(props, "GateFit", "enum", "", "", (int)cameraData.gateFit.Value);

        if (cameraData.fStop.HasValue)
            AddProperty(props, "FStop", "double", "Number", "", cameraData.fStop.Value);

        if (cameraData.focusDistance.HasValue)
            AddProperty(props, "FocusDistance", "double", "Number", "", cameraData.focusDistance.Value);

        nodeAttr.Nodes.Add(props);
        nodeAttr.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 101 } });

        GetObjectsNode().Nodes.Add(nodeAttr);

        return id;
    }

    public long AddCameraSwitcher(FbxCameraSwitcherData switcherData)
    {
        long id = GetOrCreateId(switcherData.source);

        var switcher = new FbxNode()
        {
            Name = "NodeAttribute",
            Properties = { id, switcherData.name + "\x00\x01NodeAttribute", "CameraSwitcher" }
        };

        switcher.Nodes.Add(new FbxNode() { Name = "TypeFlags", Properties = { "CameraSwitcher" } });

        var props = CreateProperties70();
        AddProperty(props, "CameraIndex", "Integer", "", "A", switcherData.cameraIndex);
        switcher.Nodes.Add(props);

        switcher.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 100 } });

        GetObjectsNode().Nodes.Add(switcher);

        return id;
    }

    public long AddNodeAttribute(string name, string attributeType, object source = null)
    {
        long id = GetOrCreateId(source ?? new object());

        var nodeAttr = new FbxNode()
        {
            Name = "NodeAttribute",
            Properties = { id, name + "\x00\x01NodeAttribute", attributeType }
        };

        nodeAttr.Nodes.Add(new FbxNode() { Name = "TypeFlags", Properties = { "Skeleton" } });

        GetObjectsNode().Nodes.Add(nodeAttr);

        return id;
    }

    public long AddLight(FbxLightData lightData)
    {
        long id = GetOrCreateId(lightData.source);

        var nodeAttr = new FbxNode()
        {
            Name = "NodeAttribute",
            Properties = { id, lightData.name + "\x00\x01NodeAttribute", "Light" }
        };

        nodeAttr.Nodes.Add(new FbxNode() { Name = "TypeFlags", Properties = { "Light" } });

        var props = CreateProperties70();

        int lightType = lightData.type == LightType.Point ? 0 :
            lightData.type == LightType.Directional ? 1 :
            lightData.type == LightType.Spot ? 2 : 3;
        AddProperty(props, "LightType", "enum", "", "", lightType);

        AddProperty(props, "Color", "ColorRGB", "Color", "",
            lightData.color.r, lightData.color.g, lightData.color.b);
        AddProperty(props, "Intensity", "double", "Number", "", lightData.intensity * 100.0);

        if (lightData.type == LightType.Spot)
        {
            AddProperty(props, "InnerAngle", "double", "Number", "", lightData.innerConeAngle ?? (lightData.spotAngle * 0.5));
            AddProperty(props, "OuterAngle", "double", "Number", "", lightData.spotAngle);
        }

        AddProperty(props, "DecayType", "enum", "", "", (int)lightData.decayType);
        AddProperty(props, "CastLight", "bool", "", "", lightData.castLight ? 1 : 0);
        AddProperty(props, "CastShadows", "bool", "", "", lightData.castShadows ? 1 : 0);

        if (lightData.shadowColor.HasValue)
        {
            Color sc = lightData.shadowColor.Value;
            AddProperty(props, "ShadowColor", "ColorRGB", "Color", "", sc.r, sc.g, sc.b);
        }

        if (lightData.areaLightShape.HasValue)
        {
            AddProperty(props, "AreaLightShape", "enum", "", "", (int)lightData.areaLightShape.Value);
        }

        if (lightData.nearAttenuationStart.HasValue)
            AddProperty(props, "NearAttenuationStart", "double", "Number", "", lightData.nearAttenuationStart.Value);

        if (lightData.nearAttenuationEnd.HasValue)
            AddProperty(props, "NearAttenuationEnd", "double", "Number", "", lightData.nearAttenuationEnd.Value);

        if (lightData.farAttenuationStart.HasValue)
            AddProperty(props, "FarAttenuationStart", "double", "Number", "", lightData.farAttenuationStart.Value);

        if (lightData.farAttenuationEnd.HasValue)
            AddProperty(props, "FarAttenuationEnd", "double", "Number", "", lightData.farAttenuationEnd.Value);

        if (lightData.enableNearAttenuation.HasValue)
            AddProperty(props, "EnableNearAttenuation", "bool", "", "", lightData.enableNearAttenuation.Value ? 1 : 0);

        if (lightData.enableFarAttenuation.HasValue)
            AddProperty(props, "EnableFarAttenuation", "bool", "", "", lightData.enableFarAttenuation.Value ? 1 : 0);

        nodeAttr.Nodes.Add(props);
        nodeAttr.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 101 } });

        GetObjectsNode().Nodes.Add(nodeAttr);

        return id;
    }

    #endregion

    #region Public API - Deformers

    public long AddSkin(FbxSkinData skinData)
    {
        long skinId = GetOrCreateId(skinData.source);

        var skin = new FbxNode()
        {
            Name = "Deformer",
            Properties = { skinId, skinData.name + "\x00\x01Deformer", "Skin" }
        };

        skin.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 101 } });
        skin.Nodes.Add(new FbxNode() { Name = "Link_DeformAcuracy", Properties = { skinData.deformAccuracy } });

        if (skinData.skinningType != null)
        {
            skin.Nodes.Add(new FbxNode() { Name = "SkinningType", Properties = { skinData.skinningType } });
        }

        GetObjectsNode().Nodes.Add(skin);

        foreach (var cluster in skinData.clusters)
        {
            long clusterId = nextId++;

            var clusterNode = new FbxNode()
            {
                Name = "Deformer",
                Properties = { clusterId, cluster.boneName + "\x00\x01SubDeformer", "Cluster" }
            };

            clusterNode.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 100 } });
            clusterNode.Nodes.Add(new FbxNode() { Name = "UserData", Properties = { "", "" } });

            clusterNode.Nodes.Add(new FbxNode() { Name = "Indexes", Properties = { cluster.indices } });
            clusterNode.Nodes.Add(new FbxNode() { Name = "Weights", Properties = { cluster.weights } });

            clusterNode.Nodes.Add(new FbxNode()
            {
                Name = "Transform",
                Properties = { MatrixToArray(cluster.transform) }
            });
            clusterNode.Nodes.Add(new FbxNode()
            {
                Name = "TransformLink",
                Properties = { MatrixToArray(cluster.transformLink) }
            });

            if (cluster.transformAssociateModel.HasValue)
            {
                clusterNode.Nodes.Add(new FbxNode()
                {
                    Name = "TransformAssociateModel",
                    Properties = { MatrixToArray(cluster.transformAssociateModel.Value) }
                });
            }

            GetObjectsNode().Nodes.Add(clusterNode);

            connections.Add(new FbxConnection("OO", clusterId, skinId));

            if (cluster.boneId.HasValue)
            {
                connections.Add(new FbxConnection("OO", clusterId, cluster.boneId.Value));
            }
        }

        return skinId;
    }

    public long AddBlendShape(FbxBlendShapeData blendShapeData)
    {
        long blendShapeId = GetOrCreateId(blendShapeData.source);

        var blendShape = new FbxNode()
        {
            Name = "Deformer",
            Properties = { blendShapeId, blendShapeData.name + "\x00\x01Deformer", "BlendShape" }
        };

        blendShape.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 100 } });

        GetObjectsNode().Nodes.Add(blendShape);

        foreach (var channel in blendShapeData.channels)
        {
            long channelId = nextId++;

            var channelNode = new FbxNode()
            {
                Name = "Deformer",
                Properties = { channelId, channel.name + "\x00\x01SubDeformer", "BlendShapeChannel" }
            };

            channelNode.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 100 } });
            channelNode.Nodes.Add(new FbxNode() { Name = "DeformPercent", Properties = { channel.deformPercent } });

            var fullWeights = new double[channel.targetShapes.Length];
            for (int i = 0; i < fullWeights.Length; i++)
                fullWeights[i] = channel.targetShapes[i].fullWeight;

            channelNode.Nodes.Add(new FbxNode() { Name = "FullWeights", Properties = { fullWeights } });

            GetObjectsNode().Nodes.Add(channelNode);

            connections.Add(new FbxConnection("OO", channelId, blendShapeId));

            foreach (var targetShape in channel.targetShapes)
            {
                long shapeId = nextId++;

                var shape = new FbxNode()
                {
                    Name = "Geometry",
                    Properties = { shapeId, targetShape.name + "\x00\x01Geometry", "Shape" }
                };

                shape.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 100 } });

                var deltaVertices = new double[targetShape.deltaVertices.Length * 3];
                for (int i = 0; i < targetShape.deltaVertices.Length; i++)
                {
                    Vector3 v = targetShape.deltaVertices[i];
                    deltaVertices[i * 3 + 0] = v.x;
                    deltaVertices[i * 3 + 1] = v.y;
                    deltaVertices[i * 3 + 2] = v.z;
                }
                shape.Nodes.Add(new FbxNode() { Name = "Vertices", Properties = { deltaVertices } });

                shape.Nodes.Add(new FbxNode() { Name = "Indexes", Properties = { targetShape.indices } });

                if (targetShape.deltaNormals != null && targetShape.deltaNormals.Length > 0)
                {
                    var deltaNormals = new double[targetShape.deltaNormals.Length * 3];
                    for (int i = 0; i < targetShape.deltaNormals.Length; i++)
                    {
                        Vector3 n = targetShape.deltaNormals[i];
                        deltaNormals[i * 3 + 0] = n.x;
                        deltaNormals[i * 3 + 1] = n.y;
                        deltaNormals[i * 3 + 2] = n.z;
                    }
                    shape.Nodes.Add(new FbxNode() { Name = "Normals", Properties = { deltaNormals } });
                    shape.Nodes.Add(new FbxNode() { Name = "NormalsIndex", Properties = { targetShape.indices } });
                }

                GetObjectsNode().Nodes.Add(shape);

                connections.Add(new FbxConnection("OO", shapeId, channelId));
            }
        }

        return blendShapeId;
    }

    public long AddVertexCache(FbxVertexCacheData cacheData)
    {
        long deformerId = GetOrCreateId(cacheData.source);

        var deformer = new FbxNode()
        {
            Name = "Deformer",
            Properties = { deformerId, cacheData.name + "\x00\x01Deformer", "VertexCache" }
        };

        deformer.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 100 } });
        deformer.Nodes.Add(new FbxNode() { Name = "CacheChannel", Properties = { cacheData.channel } });

        GetObjectsNode().Nodes.Add(deformer);

        long cacheId = nextId++;

        var cache = new FbxNode()
        {
            Name = "Cache",
            Properties = { cacheId, cacheData.cacheName + "\x00\x01Cache", "" }
        };

        cache.Nodes.Add(new FbxNode() { Name = "Type", Properties = { cacheData.cacheType } });
        cache.Nodes.Add(new FbxNode() { Name = "CacheFile", Properties = { cacheData.cacheFile } });

        GetObjectsNode().Nodes.Add(cache);

        connections.Add(new FbxConnection("OO", cacheId, deformerId));

        return deformerId;
    }

    #endregion

    #region Public API - Animation

    public long AddAnimationStack(FbxAnimationStackData stackData)
    {
        long id = GetOrCreateId(stackData.source);

        var stack = new FbxNode()
        {
            Name = "AnimationStack",
            Properties = { id, stackData.name + "\x00\x01AnimStack", "" }
        };

        var props = CreateProperties70();
        AddProperty(props, "LocalStart", "KTime", "Time", "", stackData.localStart);
        AddProperty(props, "LocalStop", "KTime", "Time", "", stackData.localStop);
        AddProperty(props, "ReferenceStart", "KTime", "Time", "", stackData.referenceStart);
        AddProperty(props, "ReferenceStop", "KTime", "Time", "", stackData.referenceStop);
        stack.Nodes.Add(props);

        GetObjectsNode().Nodes.Add(stack);

        return id;
    }

    public long AddAnimationLayer(FbxAnimationLayerData layerData)
    {
        long id = GetOrCreateId(layerData.source);

        var layer = new FbxNode()
        {
            Name = "AnimationLayer",
            Properties = { id, layerData.name + "\x00\x01AnimLayer", "" }
        };

        var props = CreateProperties70();

        if (layerData.weight.HasValue)
            AddProperty(props, "Weight", "Number", "", "A", layerData.weight.Value);

        if (layerData.mute.HasValue)
            AddProperty(props, "Mute", "bool", "", "", layerData.mute.Value ? 1 : 0);

        if (layerData.solo.HasValue)
            AddProperty(props, "Solo", "bool", "", "", layerData.solo.Value ? 1 : 0);

        if (layerData._lock.HasValue)
            AddProperty(props, "Lock", "bool", "", "", layerData._lock.Value ? 1 : 0);

        if (layerData.blendMode.HasValue)
            AddProperty(props, "BlendMode", "enum", "", "", (int)layerData.blendMode.Value);

        if (layerData.rotationBlendMode.HasValue)
            AddProperty(props, "RotationBlendMode", "enum", "", "", (int)layerData.rotationBlendMode.Value);

        if (layerData.scaleBlendMode.HasValue)
            AddProperty(props, "ScaleBlendMode", "enum", "", "", (int)layerData.scaleBlendMode.Value);

        layer.Nodes.Add(props);

        GetObjectsNode().Nodes.Add(layer);

        return id;
    }

    public long AddAnimationCurveNode(FbxAnimationCurveNodeData curveNodeData)
    {
        long id = GetOrCreateId(curveNodeData.source);

        var curveNode = new FbxNode()
        {
            Name = "AnimationCurveNode",
            Properties = { id, curveNodeData.propertyName + "\x00\x01AnimCurveNode", "" }
        };

        var props = CreateProperties70();

        if (curveNodeData.defaultValues != null)
        {
            if (curveNodeData.defaultValues.Length == 1)
            {
                AddProperty(props, "d", "Number", "", "A", curveNodeData.defaultValues[0]);
            }
            else if (curveNodeData.defaultValues.Length == 3)
            {
                AddProperty(props, "d|X", "Number", "", "A", curveNodeData.defaultValues[0]);
                AddProperty(props, "d|Y", "Number", "", "A", curveNodeData.defaultValues[1]);
                AddProperty(props, "d|Z", "Number", "", "A", curveNodeData.defaultValues[2]);
            }
        }

        curveNode.Nodes.Add(props);

        GetObjectsNode().Nodes.Add(curveNode);

        return id;
    }

    public long AddAnimationCurve(FbxAnimationCurveData curveData)
    {
        long id = GetOrCreateId(curveData.source);

        var curve = new FbxNode()
        {
            Name = "AnimationCurve",
            Properties = { id, "\x00\x01AnimCurve", "" }
        };

        curve.Nodes.Add(new FbxNode() { Name = "Default", Properties = { curveData.defaultValue } });
        curve.Nodes.Add(new FbxNode() { Name = "KeyVer", Properties = { 4009 } });

        var keyTimes = new long[curveData.keyframes.Length];
        for (int i = 0; i < curveData.keyframes.Length; i++)
        {
            keyTimes[i] = (long)(curveData.keyframes[i].time * 46186158000.0);
        }
        curve.Nodes.Add(new FbxNode() { Name = "KeyTime", Properties = { keyTimes } });

        var keyValues = new float[curveData.keyframes.Length];
        for (int i = 0; i < curveData.keyframes.Length; i++)
        {
            keyValues[i] = curveData.keyframes[i].value;
        }
        curve.Nodes.Add(new FbxNode() { Name = "KeyValueFloat", Properties = { keyValues } });

        var keyAttrFlags = new int[curveData.keyframes.Length];
        for (int i = 0; i < curveData.keyframes.Length; i++)
        {
            keyAttrFlags[i] = (int)curveData.keyframes[i].interpolation;
        }
        curve.Nodes.Add(new FbxNode() { Name = "KeyAttrFlags", Properties = { keyAttrFlags } });

        var keyAttrDataFloat = new float[curveData.keyframes.Length * 4];
        for (int i = 0; i < curveData.keyframes.Length; i++)
        {
            var kf = curveData.keyframes[i];
            keyAttrDataFloat[i * 4 + 0] = kf.rightSlope;
            keyAttrDataFloat[i * 4 + 1] = kf.nextLeftSlope;
            keyAttrDataFloat[i * 4 + 2] = kf.rightWeight;
            keyAttrDataFloat[i * 4 + 3] = kf.nextLeftWeight;
        }
        curve.Nodes.Add(new FbxNode() { Name = "KeyAttrDataFloat", Properties = { keyAttrDataFloat } });

        var keyAttrRefCount = new int[curveData.keyframes.Length];
        for (int i = 0; i < curveData.keyframes.Length; i++)
        {
            keyAttrRefCount[i] = curveData.keyframes.Length;
        }
        curve.Nodes.Add(new FbxNode() { Name = "KeyAttrRefCount", Properties = { keyAttrRefCount } });

        GetObjectsNode().Nodes.Add(curve);

        return id;
    }

    #endregion

    #region Public API - Constraints

    public long AddPositionConstraint(FbxConstraintData constraintData)
    {
        return AddConstraintInternal(constraintData, "Position");
    }

    public long AddRotationConstraint(FbxConstraintData constraintData)
    {
        return AddConstraintInternal(constraintData, "Rotation");
    }

    public long AddScaleConstraint(FbxConstraintData constraintData)
    {
        return AddConstraintInternal(constraintData, "Scaling");
    }

    public long AddParentConstraint(FbxConstraintData constraintData)
    {
        return AddConstraintInternal(constraintData, "Parent");
    }

    public long AddAimConstraint(FbxConstraintData constraintData)
    {
        return AddConstraintInternal(constraintData, "Aim");
    }

    public long AddSingleChainIKConstraint(FbxConstraintData constraintData)
    {
        return AddConstraintInternal(constraintData, "SingleChainIK");
    }

    private long AddConstraintInternal(FbxConstraintData constraintData, string type)
    {
        long id = GetOrCreateId(constraintData.source);

        var constraint = new FbxNode()
        {
            Name = "NodeAttribute",
            Properties = { id, constraintData.name + "\x00\x01NodeAttribute", type + "Constraint" }
        };

        constraint.Nodes.Add(new FbxNode() { Name = "TypeFlags", Properties = { "Skeleton" } });

        var props = CreateProperties70();
        AddProperty(props, "Active", "bool", "", "", constraintData.active ? 1 : 0);
        AddProperty(props, "Weight", "double", "Number", "", constraintData.weight);

        if (constraintData._lock.HasValue)
        {
            AddProperty(props, "Lock", "bool", "", "", constraintData._lock.Value ? 1 : 0);
        }

        if (constraintData.constrainTranslationX.HasValue)
            AddProperty(props, "AffectTranslationX", "bool", "", "", constraintData.constrainTranslationX.Value ? 1 : 0);

        if (constraintData.constrainTranslationY.HasValue)
            AddProperty(props, "AffectTranslationY", "bool", "", "", constraintData.constrainTranslationY.Value ? 1 : 0);

        if (constraintData.constrainTranslationZ.HasValue)
            AddProperty(props, "AffectTranslationZ", "bool", "", "", constraintData.constrainTranslationZ.Value ? 1 : 0);

        if (constraintData.constrainRotationX.HasValue)
            AddProperty(props, "AffectRotationX", "bool", "", "", constraintData.constrainRotationX.Value ? 1 : 0);

        if (constraintData.constrainRotationY.HasValue)
            AddProperty(props, "AffectRotationY", "bool", "", "", constraintData.constrainRotationY.Value ? 1 : 0);

        if (constraintData.constrainRotationZ.HasValue)
            AddProperty(props, "AffectRotationZ", "bool", "", "", constraintData.constrainRotationZ.Value ? 1 : 0);

        constraint.Nodes.Add(props);
        constraint.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 100 } });

        GetObjectsNode().Nodes.Add(constraint);

        return id;
    }

    #endregion

    #region Public API - Poses

    public long AddPose(FbxPoseData poseData)
    {
        long id = GetOrCreateId(poseData.source);

        var pose = new FbxNode()
        {
            Name = "Pose",
            Properties = { id, poseData.name + "\x00\x01Pose", poseData.isBindPose ? "BindPose" : "RestPose" }
        };

        pose.Nodes.Add(new FbxNode() { Name = "Type", Properties = { poseData.isBindPose ? "BindPose" : "RestPose" } });
        pose.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 100 } });
        pose.Nodes.Add(new FbxNode() { Name = "NbPoseNodes", Properties = { poseData.nodes.Length } });

        foreach (var node in poseData.nodes)
        {
            var poseNode = new FbxNode() { Name = "PoseNode" };
            poseNode.Nodes.Add(new FbxNode() { Name = "Node", Properties = { node.nodeId } });
            poseNode.Nodes.Add(new FbxNode() { Name = "Matrix", Properties = { MatrixToArray(node.matrix) } });
            pose.Nodes.Add(poseNode);
        }

        GetObjectsNode().Nodes.Add(pose);

        return id;
    }

    #endregion

    #region Public API - Collections & Scene Organization

    public long AddCollection(FbxCollectionData collectionData)
    {
        long id = GetOrCreateId(collectionData.source);

        var collection = new FbxNode()
        {
            Name = "Collection",
            Properties = { id, collectionData.name + "\x00\x01Collection", "" }
        };

        GetObjectsNode().Nodes.Add(collection);

        return id;
    }

    public long AddDisplayLayer(FbxDisplayLayerData layerData)
    {
        long id = GetOrCreateId(layerData.source);

        var displayLayer = new FbxNode()
        {
            Name = "DisplayLayer",
            Properties = { id, layerData.name + "\x00\x01DisplayLayer", "" }
        };

        var props = CreateProperties70();

        if (layerData.color.HasValue)
        {
            Color c = layerData.color.Value;
            AddProperty(props, "Color", "ColorRGB", "Color", "", c.r, c.g, c.b);
        }

        AddProperty(props, "Show", "bool", "", "", layerData.show ? 1 : 0);
        AddProperty(props, "Freeze", "bool", "", "", layerData.freeze ? 1 : 0);
        AddProperty(props, "LODBox", "bool", "", "", layerData.lodBox ? 1 : 0);

        displayLayer.Nodes.Add(props);

        GetObjectsNode().Nodes.Add(displayLayer);

        return id;
    }

    public long AddSelectionNode(FbxSelectionNodeData selectionData)
    {
        long id = GetOrCreateId(selectionData.source);

        var selection = new FbxNode()
        {
            Name = "SelectionNode",
            Properties = { id, selectionData.name + "\x00\x01SelectionNode", "" }
        };

        GetObjectsNode().Nodes.Add(selection);

        return id;
    }

    public long AddSelectionSet(FbxSelectionSetData selectionData)
    {
        long id = GetOrCreateId(selectionData.source);

        var selectionSet = new FbxNode()
        {
            Name = "SelectionSet",
            Properties = { id, selectionData.name + "\x00\x01SelectionSet", "" }
        };

        GetObjectsNode().Nodes.Add(selectionSet);

        return id;
    }

    #endregion

    #region Public API - Scene Info & Metadata

    public long AddSceneInfo(FbxSceneInfoData sceneData)
    {
        long id = nextId++;

        var sceneInfo = new FbxNode()
        {
            Name = "SceneInfo",
            Properties = { id, "SceneInfo\x00\x01UserData", "UserData" }
        };

        sceneInfo.Nodes.Add(new FbxNode() { Name = "Type", Properties = { "UserData" } });
        sceneInfo.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 100 } });

        var props = CreateProperties70();

        if (!string.IsNullOrEmpty(sceneData.documentUrl))
            AddProperty(props, "DocumentUrl", "KString", "Url", "", sceneData.documentUrl);

        if (!string.IsNullOrEmpty(sceneData.srcDocumentUrl))
            AddProperty(props, "SrcDocumentUrl", "KString", "Url", "", sceneData.srcDocumentUrl);

        if (!string.IsNullOrEmpty(sceneData.title))
            AddProperty(props, "Original|ApplicationVendor", "KString", "", "", sceneData.title);

        if (!string.IsNullOrEmpty(sceneData.subject))
            AddProperty(props, "Original|ApplicationName", "KString", "", "", sceneData.subject);

        if (!string.IsNullOrEmpty(sceneData.author))
            AddProperty(props, "Original|ApplicationVersion", "KString", "", "", sceneData.author);

        if (!string.IsNullOrEmpty(sceneData.keywords))
            AddProperty(props, "Keywords", "KString", "", "", sceneData.keywords);

        if (!string.IsNullOrEmpty(sceneData.revision))
            AddProperty(props, "Revision", "KString", "", "", sceneData.revision);

        if (!string.IsNullOrEmpty(sceneData.comment))
            AddProperty(props, "Comment", "KString", "", "", sceneData.comment);

        sceneInfo.Nodes.Add(props);

        GetObjectsNode().Nodes.Add(sceneInfo);

        return id;
    }

    #endregion

    #region Public API - Audio

    public long AddAudio(FbxAudioData audioData)
    {
        long id = GetOrCreateId(audioData.source);

        var audio = new FbxNode()
        {
            Name = "Audio",
            Properties = { id, audioData.name + "\x00\x01Audio", "Clip" }
        };

        audio.Nodes.Add(new FbxNode() { Name = "Type", Properties = { "Clip" } });

        var props = CreateProperties70();
        AddProperty(props, "Path", "KString", "XRefUrl", "", audioData.filePath);
        audio.Nodes.Add(props);

        audio.Nodes.Add(new FbxNode() { Name = "Filename", Properties = { audioData.filePath } });
        audio.Nodes.Add(new FbxNode() { Name = "RelativeFilename", Properties = { audioData.relativeFilePath ?? audioData.filePath } });

        if (audioData.content != null && audioData.content.Length > 0)
        {
            audio.Nodes.Add(new FbxNode() { Name = "Content", Properties = { audioData.content } });
        }

        GetObjectsNode().Nodes.Add(audio);

        return id;
    }

    #endregion

    #region Public API - Advanced Features

    public long AddCharacter(FbxCharacterData characterData)
    {
        long id = GetOrCreateId(characterData.source);

        var character = new FbxNode()
        {
            Name = "Character",
            Properties = { id, characterData.name + "\x00\x01Character", "" }
        };

        var props = CreateProperties70();

        if (characterData.scaleCompensationMode.HasValue)
            AddProperty(props, "ScaleCompensationMode", "enum", "", "", (int)characterData.scaleCompensationMode.Value);

        character.Nodes.Add(props);

        GetObjectsNode().Nodes.Add(character);

        return id;
    }

    public long AddControlSet(FbxControlSetData controlSetData)
    {
        long id = GetOrCreateId(controlSetData.source);

        var controlSet = new FbxNode()
        {
            Name = "ControlSet",
            Properties = { id, controlSetData.name + "\x00\x01ControlSet", "" }
        };

        GetObjectsNode().Nodes.Add(controlSet);

        return id;
    }

    public long AddImplementation(FbxImplementationData implementationData)
    {
        long id = GetOrCreateId(implementationData.source);

        var implementation = new FbxNode()
        {
            Name = "Implementation",
            Properties = { id, implementationData.name + "\x00\x01Implementation", "" }
        };

        implementation.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 100 } });
        implementation.Nodes.Add(new FbxNode() { Name = "Language", Properties = { implementationData.language } });
        implementation.Nodes.Add(new FbxNode() { Name = "LanguageVersion", Properties = { implementationData.languageVersion } });

        if (!string.IsNullOrEmpty(implementationData.renderAPI))
            implementation.Nodes.Add(new FbxNode() { Name = "RenderAPI", Properties = { implementationData.renderAPI } });

        if (!string.IsNullOrEmpty(implementationData.renderAPIVersion))
            implementation.Nodes.Add(new FbxNode() { Name = "RenderAPIVersion", Properties = { implementationData.renderAPIVersion } });

        GetObjectsNode().Nodes.Add(implementation);

        return id;
    }

    public long AddBindingTable(FbxBindingTableData bindingTableData)
    {
        long id = GetOrCreateId(bindingTableData.source);

        var bindingTable = new FbxNode()
        {
            Name = "BindingTable",
            Properties = { id, bindingTableData.name + "\x00\x01BindingTable", "" }
        };

        bindingTable.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 100 } });

        if (bindingTableData.entries != null)
        {
            foreach (var entry in bindingTableData.entries)
            {
                var entryNode = new FbxNode() { Name = "Entry" };
                entryNode.Nodes.Add(new FbxNode() { Name = "Source", Properties = { entry.source } });
                entryNode.Nodes.Add(new FbxNode() { Name = "Destination", Properties = { entry.destination } });
                bindingTable.Nodes.Add(entryNode);
            }
        }

        GetObjectsNode().Nodes.Add(bindingTable);

        return id;
    }

    public long AddEmbeddedData(FbxEmbeddedDataData embeddedData)
    {
        long id = GetOrCreateId(embeddedData.source);

        var embedded = new FbxNode()
        {
            Name = "EmbeddedData",
            Properties = { id, embeddedData.name + "\x00\x01EmbeddedData", "" }
        };

        embedded.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 100 } });

        var props = CreateProperties70();
        AddProperty(props, "Original", "KString", "", "", embeddedData.originalFileName);
        embedded.Nodes.Add(props);

        if (embeddedData.content != null && embeddedData.content.Length > 0)
        {
            embedded.Nodes.Add(new FbxNode() { Name = "Content", Properties = { embeddedData.content } });
        }

        GetObjectsNode().Nodes.Add(embedded);

        return id;
    }

    #endregion

    #region Public API - Connections

    public void ConnectGeometryToModel(long geometryId, long modelId)
    {
        connections.Add(new FbxConnection("OO", geometryId, modelId));
    }

    public void ConnectMaterialToModel(long materialId, long modelId)
    {
        connections.Add(new FbxConnection("OO", materialId, modelId));
    }

    public void ConnectMaterialToGeometry(long materialId, long geometryId)
    {
        connections.Add(new FbxConnection("OO", materialId, geometryId));
    }

    public void ConnectTextureToMaterial(long textureId, long materialId, string propertyName = "DiffuseColor")
    {
        connections.Add(new FbxConnection("OP", textureId, materialId, propertyName));
    }

    public void ConnectLayeredTextureToMaterial(long layeredTextureId, long materialId, string propertyName = "DiffuseColor")
    {
        connections.Add(new FbxConnection("OP", layeredTextureId, materialId, propertyName));
    }

    public void ConnectTextureToLayeredTexture(long textureId, long layeredTextureId)
    {
        connections.Add(new FbxConnection("OO", textureId, layeredTextureId));
    }

    public void ConnectVideoToTexture(long videoId, long textureId)
    {
        connections.Add(new FbxConnection("OO", videoId, textureId));
    }

    public void ConnectNodeAttributeToModel(long nodeAttributeId, long modelId)
    {
        connections.Add(new FbxConnection("OO", nodeAttributeId, modelId));
    }

    public void ConnectSkinToGeometry(long skinId, long geometryId)
    {
        connections.Add(new FbxConnection("OO", skinId, geometryId));
    }

    public void ConnectBlendShapeToGeometry(long blendShapeId, long geometryId)
    {
        connections.Add(new FbxConnection("OO", blendShapeId, geometryId));
    }

    public void ConnectVertexCacheToGeometry(long vertexCacheId, long geometryId)
    {
        connections.Add(new FbxConnection("OO", vertexCacheId, geometryId));
    }

    public void ConnectAnimationLayerToStack(long layerId, long stackId)
    {
        connections.Add(new FbxConnection("OO", layerId, stackId));
    }

    public void ConnectAnimationCurveNodeToLayer(long curveNodeId, long layerId)
    {
        connections.Add(new FbxConnection("OO", curveNodeId, layerId));
    }

    public void ConnectAnimationCurveNodeToModel(long curveNodeId, long modelId, string propertyName)
    {
        connections.Add(new FbxConnection("OP", curveNodeId, modelId, propertyName));
    }

    public void ConnectAnimationCurveToCurveNode(long curveId, long curveNodeId, string component)
    {
        connections.Add(new FbxConnection("OP", curveId, curveNodeId, component));
    }

    public void ConnectConstraintToModel(long constraintId, long modelId)
    {
        connections.Add(new FbxConnection("OO", constraintId, modelId));
    }

    public void ConnectModelToCollection(long modelId, long collectionId)
    {
        connections.Add(new FbxConnection("OO", modelId, collectionId));
    }

    public void ConnectModelToDisplayLayer(long modelId, long displayLayerId)
    {
        connections.Add(new FbxConnection("OO", modelId, displayLayerId));
    }

    public void ConnectToSelectionNode(long objectId, long selectionNodeId)
    {
        connections.Add(new FbxConnection("OO", objectId, selectionNodeId));
    }

    public void ConnectToSelectionSet(long objectId, long selectionSetId)
    {
        connections.Add(new FbxConnection("OO", objectId, selectionSetId));
    }

    public void ConnectCharacterToModel(long characterId, long modelId, string propertyName = "")
    {
        if (string.IsNullOrEmpty(propertyName))
            connections.Add(new FbxConnection("OO", characterId, modelId));
        else
            connections.Add(new FbxConnection("OP", characterId, modelId, propertyName));
    }

    public void ConnectControlSetToCharacter(long controlSetId, long characterId)
    {
        connections.Add(new FbxConnection("OO", controlSetId, characterId));
    }

    public void ConnectImplementationToMaterial(long implementationId, long materialId)
    {
        connections.Add(new FbxConnection("OO", implementationId, materialId));
    }

    public void ConnectBindingTableToImplementation(long bindingTableId, long implementationId)
    {
        connections.Add(new FbxConnection("OO", bindingTableId, implementationId));
    }

    public void ConnectAudioToModel(long audioId, long modelId)
    {
        connections.Add(new FbxConnection("OO", audioId, modelId));
    }

    #endregion

    #region Public API - Object Management

    public bool RemoveObject(long id)
    {
        var objectsNode = GetObjectsNode();
        var nodeToRemove = objectsNode.Nodes.FirstOrDefault(n =>
            n.Properties.Count > 0 && n.Properties[0] is long nodeId && nodeId == id);

        if (nodeToRemove != null)
        {
            objectsNode.Nodes.Remove(nodeToRemove);
            connections.RemoveAll(c => c.childId == id || c.parentId == id);

            var sourceToRemove = objectIds.FirstOrDefault(kvp => kvp.Value == id).Key;
            if (sourceToRemove != null)
                objectIds.Remove(sourceToRemove);

            return true;
        }

        return false;
    }

    public bool RemoveObject(object source)
    {
        if (source != null && objectIds.ContainsKey(source))
        {
            long id = objectIds[source];
            return RemoveObject(id);
        }
        return false;
    }

    public void ClearAllObjects()
    {
        GetObjectsNode().Nodes.Clear();
        connections.Clear();
        objectIds.Clear();
    }

    public long? GetObjectId(object source)
    {
        if (source != null && objectIds.ContainsKey(source))
            return objectIds[source];
        return null;
    }

    #endregion

    #region Public API - Finalize

    public FbxDocument GetDocument()
    {
        BuildDefinitions();
        BuildConnections();
        BuildTakes();
        CleanDocument();
        return doc;
    }

    private void CleanDocument()
    {
        void CleanNodeRecursive(FbxNode node)
        {
            if (node == null) return;

            if (node.Nodes != null)
            {
                var cleanedNodes = node.Nodes.Where(n => n != null).ToList();
                node.Nodes.Clear();
                foreach (var n in cleanedNodes)
                {
                    node.Nodes.Add(n);
                    CleanNodeRecursive(n);
                }
            }
        }

        if (doc.Nodes != null)
        {
            var cleanedRootNodes = doc.Nodes.Where(n => n != null).ToList();
            doc.Nodes.Clear();
            foreach (var node in cleanedRootNodes)
            {
                doc.Nodes.Add(node);
                CleanNodeRecursive(node);
            }
        }
    }

    #endregion

    #region Helper Methods - Data Extraction

    public static FbxModelData ExtractModel(Transform transform, long? parentId = null, string modelType = "Null")
    {
        return new FbxModelData
        {
            source = transform.gameObject,
            name = transform.name,
            modelType = modelType,
            localPosition = transform.localPosition,
            localRotation = transform.localEulerAngles,
            localScale = transform.localScale,
            parentId = parentId,
            shading = 1,  culling = "CullingOff"
        };
    }
    public static FbxGeometryData ExtractGeometry(Mesh mesh)
    {
        return new FbxGeometryData
        {
            source = mesh,
            name = mesh.name,
            vertices = mesh.vertices,
            normals = mesh.normals,
            uvChannels = new[] { mesh.uv, mesh.uv2, mesh.uv3, mesh.uv4 },
            colorChannels = mesh.colors.Length > 0 ? new[] { mesh.colors } : null,
            triangles = mesh.triangles,
            tangents = mesh.tangents.Length > 0 ? mesh.tangents : null,
            materialIndices = ExtractMaterialIndices(mesh)
        };
    }

    public static FbxMaterialData ExtractMaterial(Material material)
    {
        var data = new FbxMaterialData
        {
            source = material,
            name = material.name,
            shadingModel = "phong"
        };

        if (material.HasProperty("_Color"))
            data.diffuseColor = material.GetColor("_Color");

        if (material.HasProperty("_SpecColor"))
            data.specularColor = material.GetColor("_SpecColor");

        if (material.HasProperty("_EmissionColor"))
            data.emissiveColor = material.GetColor("_EmissionColor");

        if (material.HasProperty("_Glossiness"))
            data.shininess = material.GetFloat("_Glossiness") * 100.0;

        if (material.HasProperty("_Metallic"))
            data.metallic = material.GetFloat("_Metallic");

        return data;
    }

    public static FbxTextureData ExtractTexture(Texture2D texture, string filePath)
    {
        return new FbxTextureData
        {
            source = texture,
            name = texture.name,
            filePath = filePath,
            uvSet = "UVChannel_1"
        };
    }

    public static FbxVideoData ExtractVideo(Texture2D texture, string filePath, byte[] content = null)
    {
        return new FbxVideoData
        {
            source = texture,
            name = texture.name,
            filePath = filePath,
            width = texture.width,
            height = texture.height,
            content = content
        };
    }

    public static FbxCameraData ExtractCamera(Camera camera)
    {
        return new FbxCameraData
        {
            source = camera,
            name = camera.name,
            position = Vector3.zero,
            upVector = Vector3.up,
            targetPosition = Vector3.forward * 10f,
            fieldOfView = camera.fieldOfView,
            focalLength = Camera.FieldOfViewToFocalLength(camera.fieldOfView, camera.sensorSize.y),
            nearClipPlane = camera.nearClipPlane,
            farClipPlane = camera.farClipPlane,
            isOrthographic = camera.orthographic,
            orthographicSize = camera.orthographicSize,
            aspectRatio = camera.aspect,
            filmWidth = camera.sensorSize.x,
            filmHeight = camera.sensorSize.y
        };
    }

    public static FbxLightData ExtractLight(Light light)
    {
        return new FbxLightData
        {
            source = light,
            name = light.name,
            type = light.type,
            color = light.color,
            intensity = light.intensity,
            spotAngle = light.spotAngle,
            range = light.range,
            castLight = true,
            castShadows = light.shadows != LightShadows.None,
            decayType = FbxLightDecayType.QuadraticDecay
        };
    }

    public static FbxSkinData ExtractSkin(SkinnedMeshRenderer skinnedMesh, Dictionary<Transform, long> boneToId)
    {
        var mesh = skinnedMesh.sharedMesh;
        var bones = skinnedMesh.bones;
        var bindposes = mesh.bindposes;

        var clusters = new List<FbxClusterData>();

        for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
        {
            var bone = bones[boneIndex];
            if (bone == null) continue;

            var vertexIndices = new List<int>();
            var weights = new List<double>();

            var boneWeights = mesh.boneWeights;
            for (int vertexIndex = 0; vertexIndex < boneWeights.Length; vertexIndex++)
            {
                var bw = boneWeights[vertexIndex];

                if (bw.boneIndex0 == boneIndex && bw.weight0 > 0)
                {
                    vertexIndices.Add(vertexIndex);
                    weights.Add(bw.weight0);
                }
                else if (bw.boneIndex1 == boneIndex && bw.weight1 > 0)
                {
                    vertexIndices.Add(vertexIndex);
                    weights.Add(bw.weight1);
                }
                else if (bw.boneIndex2 == boneIndex && bw.weight2 > 0)
                {
                    vertexIndices.Add(vertexIndex);
                    weights.Add(bw.weight2);
                }
                else if (bw.boneIndex3 == boneIndex && bw.weight3 > 0)
                {
                    vertexIndices.Add(vertexIndex);
                    weights.Add(bw.weight3);
                }
            }

            if (vertexIndices.Count > 0)
            {
                long? boneId = null;
                if (boneToId != null && boneToId.ContainsKey(bone))
                    boneId = boneToId[bone];

                clusters.Add(new FbxClusterData
                {
                    boneName = bone.name,
                    boneId = boneId,
                    indices = vertexIndices.ToArray(),
                    weights = weights.ToArray(),
                    transform = skinnedMesh.transform.worldToLocalMatrix,
                    transformLink = bindposes[boneIndex]
                });
            }
        }

        return new FbxSkinData
        {
            source = skinnedMesh,
            name = skinnedMesh.name + "_Skin",
            deformAccuracy = 50.0,
            skinningType = "Linear",
            clusters = clusters.ToArray()
        };
    }

    public static FbxPoseData ExtractBindPose(SkinnedMeshRenderer skinnedMesh, Dictionary<Transform, long> boneToId)
    {
        var mesh = skinnedMesh.sharedMesh;
        var bones = skinnedMesh.bones;
        var bindposes = mesh.bindposes;

        var poseNodes = new List<FbxPoseNodeData>();

        long? meshModelId = boneToId != null && boneToId.ContainsKey(skinnedMesh.transform)
            ? boneToId[skinnedMesh.transform]
            : (long?)null;

        if (meshModelId.HasValue)
        {
            poseNodes.Add(new FbxPoseNodeData
            {
                nodeId = meshModelId.Value,
                matrix = skinnedMesh.transform.localToWorldMatrix
            });
        }

        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] == null) continue;

            long? boneId = boneToId != null && boneToId.ContainsKey(bones[i])
                ? boneToId[bones[i]]
                : (long?)null;

            if (boneId.HasValue)
            {
                poseNodes.Add(new FbxPoseNodeData
                {
                    nodeId = boneId.Value,
                    matrix = bones[i].localToWorldMatrix
                });
            }
        }

        return new FbxPoseData
        {
            name = "BindPose",
            isBindPose = true,
            nodes = poseNodes.ToArray()
        };
    }

    public static FbxBlendShapeData ExtractBlendShape(Mesh mesh)
    {
        var channels = new List<FbxBlendShapeChannelData>();

        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
            string shapeName = mesh.GetBlendShapeName(i);
            int frameCount = mesh.GetBlendShapeFrameCount(i);

            var targetShapes = new List<FbxShapeData>();

            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                float weight = mesh.GetBlendShapeFrameWeight(i, frameIndex);

                var deltaVertices = new Vector3[mesh.vertexCount];
                var deltaNormals = new Vector3[mesh.vertexCount];
                var deltaTangents = new Vector3[mesh.vertexCount];

                mesh.GetBlendShapeFrameVertices(i, frameIndex, deltaVertices, deltaNormals, deltaTangents);

                var indices = new List<int>();
                var compactDeltas = new List<Vector3>();
                var compactNormals = new List<Vector3>();

                for (int v = 0; v < deltaVertices.Length; v++)
                {
                    if (deltaVertices[v].sqrMagnitude > 0.0001f)
                    {
                        indices.Add(v);
                        compactDeltas.Add(deltaVertices[v]);
                        compactNormals.Add(deltaNormals[v]);
                    }
                }

                targetShapes.Add(new FbxShapeData
                {
                    name = shapeName + "_" + frameIndex,
                    indices = indices.ToArray(),
                    deltaVertices = compactDeltas.ToArray(),
                    deltaNormals = compactNormals.ToArray(),
                    fullWeight = weight
                });
            }

            channels.Add(new FbxBlendShapeChannelData
            {
                name = shapeName,
                deformPercent = 0.0,
                targetShapes = targetShapes.ToArray()
            });
        }

        return new FbxBlendShapeData
        {
            source = mesh,
            name = mesh.name + "_BlendShape",
            channels = channels.ToArray()
        };
    }

    public static FbxAnimationStackData CreateAnimationStack(string name, double startTime, double endTime)
    {
        long fbxStartTime = (long)(startTime * 46186158000.0);
        long fbxEndTime = (long)(endTime * 46186158000.0);

        return new FbxAnimationStackData
        {
            name = name,
            localStart = fbxStartTime,
            localStop = fbxEndTime,
            referenceStart = fbxStartTime,
            referenceStop = fbxEndTime
        };
    }

    public static FbxAnimationCurveData CreateAnimationCurve(AnimationCurve unityCurve, double timeScale = 1.0)
    {
        var keyframes = new FbxKeyframe[unityCurve.length];

        for (int i = 0; i < unityCurve.length; i++)
        {
            var key = unityCurve.keys[i];

            keyframes[i] = new FbxKeyframe
            {
                time = key.time * timeScale,
                value = key.value,
                interpolation = FbxInterpolationType.Cubic,
                rightSlope = key.outTangent,
                nextLeftSlope = i < unityCurve.length - 1 ? unityCurve.keys[i + 1].inTangent : 0,
                rightWeight = key.outWeight,
                nextLeftWeight = i < unityCurve.length - 1 ? unityCurve.keys[i + 1].inWeight : 0
            };
        }

        return new FbxAnimationCurveData
        {
            keyframes = keyframes,
            defaultValue = unityCurve.Evaluate(0)
        };
    }

    private static int[] ExtractMaterialIndices(Mesh mesh)
    {
        if (mesh.subMeshCount <= 1)
            return null;

        int triangleCount = mesh.triangles.Length / 3;
        int[] materialIndices = new int[triangleCount];

        int offset = 0;
        for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
        {
            int[] subMeshTriangles = mesh.GetTriangles(subMesh);
            int subMeshTriangleCount = subMeshTriangles.Length / 3;

            for (int i = 0; i < subMeshTriangleCount; i++)
            {
                materialIndices[offset + i] = subMesh;
            }

            offset += subMeshTriangleCount;
        }

        return materialIndices;
    }

    #endregion

    #region Internal Implementation

    private void InitializeDocument()
    {
        var headerExt = new FbxNode() { Name = "FBXHeaderExtension" };
        headerExt.Nodes.Add(new FbxNode() { Name = "FBXHeaderVersion", Properties = { 1003 } });
        headerExt.Nodes.Add(new FbxNode() { Name = "FBXVersion", Properties = { 7400 } });
        headerExt.Nodes.Add(new FbxNode() { Name = "EncryptionType", Properties = { 0 } });

        var creationTimeStamp = new FbxNode() { Name = "CreationTimeStamp" };
        var now = System.DateTime.Now;
        creationTimeStamp.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 1000 } });
        creationTimeStamp.Nodes.Add(new FbxNode() { Name = "Year", Properties = { now.Year } });
        creationTimeStamp.Nodes.Add(new FbxNode() { Name = "Month", Properties = { now.Month } });
        creationTimeStamp.Nodes.Add(new FbxNode() { Name = "Day", Properties = { now.Day } });
        creationTimeStamp.Nodes.Add(new FbxNode() { Name = "Hour", Properties = { now.Hour } });
        creationTimeStamp.Nodes.Add(new FbxNode() { Name = "Minute", Properties = { now.Minute } });
        creationTimeStamp.Nodes.Add(new FbxNode() { Name = "Second", Properties = { now.Second } });
        creationTimeStamp.Nodes.Add(new FbxNode() { Name = "Millisecond", Properties = { now.Millisecond } });
        headerExt.Nodes.Add(creationTimeStamp);

        string creatorString = "Unity FBX Exporter";
        headerExt.Nodes.Add(new FbxNode() { Name = "Creator", Properties = { creatorString } });

        var sceneInfo = new FbxNode()
        {
            Name = "SceneInfo",
            Properties = { "GlobalInfo\x00\x01SceneInfo", "UserData" }
        };
        sceneInfo.Nodes.Add(new FbxNode() { Name = "Type", Properties = { "UserData" } });
        sceneInfo.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 100 } });

        var metaData = new FbxNode() { Name = "MetaData" };
        metaData.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 100 } });
        metaData.Nodes.Add(new FbxNode() { Name = "Title", Properties = { "" } });
        metaData.Nodes.Add(new FbxNode() { Name = "Subject", Properties = { "" } });
        metaData.Nodes.Add(new FbxNode() { Name = "Author", Properties = { "" } });
        metaData.Nodes.Add(new FbxNode() { Name = "Keywords", Properties = { "" } });
        metaData.Nodes.Add(new FbxNode() { Name = "Revision", Properties = { "" } });
        metaData.Nodes.Add(new FbxNode() { Name = "Comment", Properties = { "" } });
        sceneInfo.Nodes.Add(metaData);

        var sceneInfoProps = CreateProperties70();
        AddProperty(sceneInfoProps, "DocumentUrl", "KString", "Url", "", "/foobar.fbx");
        AddProperty(sceneInfoProps, "SrcDocumentUrl", "KString", "Url", "", "/foobar.fbx");
        AddProperty(sceneInfoProps, "Original|ApplicationVendor", "KString", "", "", "Unity Technologies");
        AddProperty(sceneInfoProps, "Original|ApplicationName", "KString", "", "", "Unity");
        AddProperty(sceneInfoProps, "Original|ApplicationVersion", "KString", "", "", Application.unityVersion);
        AddProperty(sceneInfoProps, "Original|DateTime_GMT", "DateTime", "", "", "01/01/1970 00:00:00.000");
        AddProperty(sceneInfoProps, "Original|FileName", "KString", "", "", "/foobar.fbx");
        AddProperty(sceneInfoProps, "LastSaved|ApplicationVendor", "KString", "", "", "Unity Technologies");
        AddProperty(sceneInfoProps, "LastSaved|ApplicationName", "KString", "", "", "Unity");
        AddProperty(sceneInfoProps, "LastSaved|ApplicationVersion", "KString", "", "", Application.unityVersion);
        AddProperty(sceneInfoProps, "LastSaved|DateTime_GMT", "DateTime", "", "", "01/01/1970 00:00:00.000");
        sceneInfo.Nodes.Add(sceneInfoProps);

        headerExt.Nodes.Add(sceneInfo);
        doc.Nodes.Add(headerExt);

        doc.Nodes.Add(new FbxNode()
        {
            Name = "FileId",
            Properties =
            {
                new byte[]
                {
                    0x28, 0xb3, 0x2a, 0xeb, 0xb6, 0x24, 0xcc, 0xc2,
                    0xbf, 0xc8, 0xb0, 0x2a, 0xa9, 0x2b, 0xfc, 0xf1
                }
            }
        });

        doc.Nodes.Add(new FbxNode()
        {
            Name = "CreationTime",
            Properties = { now.ToString("yyyy-MM-dd HH:mm:ss:fff") }
        });

        doc.Nodes.Add(new FbxNode() { Name = "Creator", Properties = { creatorString } });

        var globalSettings = new FbxNode() { Name = "GlobalSettings" };
        globalSettings.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 1000 } });

        var gsProps = CreateProperties70();
        AddProperty(gsProps, "UpAxis", "int", "Integer", "", 1);
        AddProperty(gsProps, "UpAxisSign", "int", "Integer", "", 1);
        AddProperty(gsProps, "FrontAxis", "int", "Integer", "", 2);
        AddProperty(gsProps, "FrontAxisSign", "int", "Integer", "", 1);
        AddProperty(gsProps, "CoordAxis", "int", "Integer", "", 0);
        AddProperty(gsProps, "CoordAxisSign", "int", "Integer", "", 1);
        AddProperty(gsProps, "OriginalUpAxis", "int", "Integer", "", -1);
        AddProperty(gsProps, "OriginalUpAxisSign", "int", "Integer", "", 1);
        AddProperty(gsProps, "UnitScaleFactor", "double", "Number", "", 1.0);
        AddProperty(gsProps, "OriginalUnitScaleFactor", "double", "Number", "", 1.0);
        AddProperty(gsProps, "AmbientColor", "ColorRGB", "Color", "", 0.0, 0.0, 0.0);
        AddProperty(gsProps, "DefaultCamera", "KString", "", "", "Producer Perspective");
        AddProperty(gsProps, "TimeMode", "enum", "", "", 0);
        AddProperty(gsProps, "TimeSpanStart", "KTime", "Time", "", 0L);
        AddProperty(gsProps, "TimeSpanStop", "KTime", "Time", "", 46186158000L);
        AddProperty(gsProps, "CustomFrameRate", "double", "Number", "", -1.0);

        globalSettings.Nodes.Add(gsProps);
        doc.Nodes.Add(globalSettings);

        var documents = new FbxNode() { Name = "Documents" };
        documents.Nodes.Add(new FbxNode() { Name = "Count", Properties = { 1 } });

        var document = new FbxNode()
        {
            Name = "Document",
            Properties = { 1234567890L, "Scene", "Scene" }
        };

        var docProps = CreateProperties70();
        AddProperty(docProps, "SourceObject", "object", "", "");
        AddProperty(docProps, "ActiveAnimStackName", "KString", "", "", "");
        document.Nodes.Add(docProps);
        document.Nodes.Add(new FbxNode() { Name = "RootNode", Properties = { 0L } });

        documents.Nodes.Add(document);
        doc.Nodes.Add(documents);

        doc.Nodes.Add(new FbxNode() { Name = "References" });

        doc.Nodes.Add(new FbxNode() { Name = "Objects" });
    }

    private void BuildDefinitions()
    {
        var definitions = new FbxNode() { Name = "Definitions" };
        definitions.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 100 } });

        var objectCounts = new Dictionary<string, int>();
        var objectSubTypes = new Dictionary<string, HashSet<string>>();

        foreach (var node in GetObjectsNode().Nodes)
        {
            if (node == null) continue;

            string fbxClassName = node.Name;

            string subType = "";
            if (node.Properties.Count >= 3 && node.Properties[2] is string st)
                subType = st;

            if (!objectCounts.ContainsKey(fbxClassName))
            {
                objectCounts[fbxClassName] = 0;
                objectSubTypes[fbxClassName] = new HashSet<string>();
            }
            objectCounts[fbxClassName]++;
            if (!string.IsNullOrEmpty(subType))
                objectSubTypes[fbxClassName].Add(subType);
        }

        int totalCount = 1 + objectCounts.Values.Sum();
        definitions.Nodes.Add(new FbxNode() { Name = "Count", Properties = { totalCount } });

        var globalSettingsDef = new FbxNode() { Name = "ObjectType", Properties = { "GlobalSettings" } };
        globalSettingsDef.Nodes.Add(new FbxNode() { Name = "Count", Properties = { 1 } });
        definitions.Nodes.Add(globalSettingsDef);

        foreach (var kvp in objectCounts)
        {
            var objectType = new FbxNode() { Name = "ObjectType", Properties = { kvp.Key } };
            objectType.Nodes.Add(new FbxNode() { Name = "Count", Properties = { kvp.Value } });

            string templateName = GetPropertyTemplateName(kvp.Key, objectSubTypes[kvp.Key]);
            if (templateName != null)
            {
                var propTemplate = new FbxNode() { Name = "PropertyTemplate", Properties = { templateName } };
                var templateProps = CreateProperties70();
                PopulatePropertyTemplate(templateProps, kvp.Key, templateName);
                propTemplate.Nodes.Add(templateProps);
                objectType.Nodes.Add(propTemplate);
            }

            definitions.Nodes.Add(objectType);
        }

        int objectsIndex = doc.Nodes.FindIndex(n => n.Name == "Objects");
        if (objectsIndex >= 0)
            doc.Nodes.Insert(objectsIndex, definitions);
        else
            doc.Nodes.Add(definitions);
    }

    private void PopulatePropertyTemplate(FbxNode templateProps, string fbxClassName, string templateName)
    {
        switch (templateName)
        {
            case "FbxNode":
                AddProperty(templateProps, "QuaternionInterpolate", "enum", "", "", 0);
                AddProperty(templateProps, "RotationOffset", "Vector3D", "Vector", "", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "RotationPivot", "Vector3D", "Vector", "", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "ScalingOffset", "Vector3D", "Vector", "", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "ScalingPivot", "Vector3D", "Vector", "", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "TranslationActive", "bool", "", "", 0);
                AddProperty(templateProps, "TranslationMin", "Vector3D", "Vector", "", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "TranslationMax", "Vector3D", "Vector", "", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "TranslationMinX", "bool", "", "", 0);
                AddProperty(templateProps, "TranslationMinY", "bool", "", "", 0);
                AddProperty(templateProps, "TranslationMinZ", "bool", "", "", 0);
                AddProperty(templateProps, "TranslationMaxX", "bool", "", "", 0);
                AddProperty(templateProps, "TranslationMaxY", "bool", "", "", 0);
                AddProperty(templateProps, "TranslationMaxZ", "bool", "", "", 0);
                AddProperty(templateProps, "RotationOrder", "enum", "", "", 0);
                AddProperty(templateProps, "RotationSpaceForLimitOnly", "bool", "", "", 0);
                AddProperty(templateProps, "RotationStiffnessX", "double", "Number", "", 0.0);
                AddProperty(templateProps, "RotationStiffnessY", "double", "Number", "", 0.0);
                AddProperty(templateProps, "RotationStiffnessZ", "double", "Number", "", 0.0);
                AddProperty(templateProps, "AxisLen", "double", "Number", "", 10.0);
                AddProperty(templateProps, "PreRotation", "Vector3D", "Vector", "", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "PostRotation", "Vector3D", "Vector", "", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "RotationActive", "bool", "", "", 0);
                AddProperty(templateProps, "RotationMin", "Vector3D", "Vector", "", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "RotationMax", "Vector3D", "Vector", "", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "RotationMinX", "bool", "", "", 0);
                AddProperty(templateProps, "RotationMinY", "bool", "", "", 0);
                AddProperty(templateProps, "RotationMinZ", "bool", "", "", 0);
                AddProperty(templateProps, "RotationMaxX", "bool", "", "", 0);
                AddProperty(templateProps, "RotationMaxY", "bool", "", "", 0);
                AddProperty(templateProps, "RotationMaxZ", "bool", "", "", 0);
                AddProperty(templateProps, "InheritType", "enum", "", "", 0);
                AddProperty(templateProps, "ScalingActive", "bool", "", "", 0);
                AddProperty(templateProps, "ScalingMin", "Vector3D", "Vector", "", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "ScalingMax", "Vector3D", "Vector", "", 1.0, 1.0, 1.0);
                AddProperty(templateProps, "ScalingMinX", "bool", "", "", 0);
                AddProperty(templateProps, "ScalingMinY", "bool", "", "", 0);
                AddProperty(templateProps, "ScalingMinZ", "bool", "", "", 0);
                AddProperty(templateProps, "ScalingMaxX", "bool", "", "", 0);
                AddProperty(templateProps, "ScalingMaxY", "bool", "", "", 0);
                AddProperty(templateProps, "ScalingMaxZ", "bool", "", "", 0);
                AddProperty(templateProps, "GeometricTranslation", "Vector3D", "Vector", "", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "GeometricRotation", "Vector3D", "Vector", "", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "GeometricScaling", "Vector3D", "Vector", "", 1.0, 1.0, 1.0);
                AddProperty(templateProps, "MinDampRangeX", "double", "Number", "", 0.0);
                AddProperty(templateProps, "MinDampRangeY", "double", "Number", "", 0.0);
                AddProperty(templateProps, "MinDampRangeZ", "double", "Number", "", 0.0);
                AddProperty(templateProps, "MaxDampRangeX", "double", "Number", "", 0.0);
                AddProperty(templateProps, "MaxDampRangeY", "double", "Number", "", 0.0);
                AddProperty(templateProps, "MaxDampRangeZ", "double", "Number", "", 0.0);
                AddProperty(templateProps, "MinDampStrengthX", "double", "Number", "", 0.0);
                AddProperty(templateProps, "MinDampStrengthY", "double", "Number", "", 0.0);
                AddProperty(templateProps, "MinDampStrengthZ", "double", "Number", "", 0.0);
                AddProperty(templateProps, "MaxDampStrengthX", "double", "Number", "", 0.0);
                AddProperty(templateProps, "MaxDampStrengthY", "double", "Number", "", 0.0);
                AddProperty(templateProps, "MaxDampStrengthZ", "double", "Number", "", 0.0);
                AddProperty(templateProps, "PreferedAngleX", "double", "Number", "", 0.0);
                AddProperty(templateProps, "PreferedAngleY", "double", "Number", "", 0.0);
                AddProperty(templateProps, "PreferedAngleZ", "double", "Number", "", 0.0);
                AddProperty(templateProps, "LookAtProperty", "object", "", "");
                AddProperty(templateProps, "UpVectorProperty", "object", "", "");
                AddProperty(templateProps, "Show", "bool", "", "", 1);
                AddProperty(templateProps, "NegativePercentShapeSupport", "bool", "", "", 1);
                AddProperty(templateProps, "DefaultAttributeIndex", "int", "Integer", "", -1);
                AddProperty(templateProps, "Freeze", "bool", "", "", 0);
                AddProperty(templateProps, "LODBox", "bool", "", "", 0);
                AddProperty(templateProps, "Lcl Translation", "Lcl Translation", "", "A", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "Lcl Rotation", "Lcl Rotation", "", "A", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "Lcl Scaling", "Lcl Scaling", "", "A", 1.0, 1.0, 1.0);
                AddProperty(templateProps, "Visibility", "Visibility", "", "A", 1.0);
                AddProperty(templateProps, "Visibility Inheritance", "Visibility Inheritance", "", "", 1);
                break;

            case "FbxMesh":
                AddProperty(templateProps, "Color", "ColorRGB", "Color", "", 0.8, 0.8, 0.8);
                AddProperty(templateProps, "BBoxMin", "Vector3D", "Vector", "", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "BBoxMax", "Vector3D", "Vector", "", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "Primary Visibility", "bool", "", "", 1);
                AddProperty(templateProps, "Casts Shadows", "bool", "", "", 1);
                AddProperty(templateProps, "Receive Shadows", "bool", "", "", 1);
                break;

            case "FbxSurfacePhong":
                AddProperty(templateProps, "ShadingModel", "KString", "", "", "Phong");
                AddProperty(templateProps, "MultiLayer", "bool", "", "", 0);
                AddProperty(templateProps, "EmissiveColor", "Color", "", "A", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "EmissiveFactor", "Number", "", "A", 1.0);
                AddProperty(templateProps, "AmbientColor", "Color", "", "A", 0.2, 0.2, 0.2);
                AddProperty(templateProps, "AmbientFactor", "Number", "", "A", 1.0);
                AddProperty(templateProps, "DiffuseColor", "Color", "", "A", 0.8, 0.8, 0.8);
                AddProperty(templateProps, "DiffuseFactor", "Number", "", "A", 1.0);
                AddProperty(templateProps, "Bump", "Vector3D", "Vector", "", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "NormalMap", "Vector3D", "Vector", "", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "BumpFactor", "double", "Number", "", 1.0);
                AddProperty(templateProps, "TransparentColor", "Color", "", "A", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "TransparencyFactor", "Number", "", "A", 0.0);
                AddProperty(templateProps, "DisplacementColor", "ColorRGB", "Color", "", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "DisplacementFactor", "double", "Number", "", 1.0);
                AddProperty(templateProps, "VectorDisplacementColor", "ColorRGB", "Color", "", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "VectorDisplacementFactor", "double", "Number", "", 1.0);
                AddProperty(templateProps, "SpecularColor", "Color", "", "A", 0.2, 0.2, 0.2);
                AddProperty(templateProps, "SpecularFactor", "Number", "", "A", 1.0);
                AddProperty(templateProps, "ShininessExponent", "Number", "", "A", 20.0);
                AddProperty(templateProps, "ReflectionColor", "Color", "", "A", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "ReflectionFactor", "Number", "", "A", 1.0);
                break;

            case "FbxFileTexture":
                AddProperty(templateProps, "TextureTypeUse", "enum", "", "", 0);
                AddProperty(templateProps, "Texture alpha", "Number", "", "A", 1.0);
                AddProperty(templateProps, "CurrentMappingType", "enum", "", "", 0);
                AddProperty(templateProps, "WrapModeU", "enum", "", "", 0);
                AddProperty(templateProps, "WrapModeV", "enum", "", "", 0);
                AddProperty(templateProps, "UVSwap", "bool", "", "", 0);
                AddProperty(templateProps, "PremultiplyAlpha", "bool", "", "", 1);
                AddProperty(templateProps, "Translation", "Vector", "", "A", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "Rotation", "Vector", "", "A", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "Scaling", "Vector", "", "A", 1.0, 1.0, 1.0);
                AddProperty(templateProps, "TextureRotationPivot", "Vector3D", "Vector", "", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "TextureScalingPivot", "Vector3D", "Vector", "", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "CurrentTextureBlendMode", "enum", "", "", 1);
                AddProperty(templateProps, "UVSet", "KString", "", "", "default");
                AddProperty(templateProps, "UseMaterial", "bool", "", "", 0);
                AddProperty(templateProps, "UseMipMap", "bool", "", "", 0);
                break;

            case "FbxVideo":
                AddProperty(templateProps, "ImageSequence", "bool", "", "", 0);
                AddProperty(templateProps, "ImageSequenceOffset", "int", "Integer", "", 0);
                AddProperty(templateProps, "FrameRate", "double", "Number", "", 0.0);
                AddProperty(templateProps, "LastFrame", "int", "Integer", "", 0);
                AddProperty(templateProps, "Width", "int", "Integer", "", 0);
                AddProperty(templateProps, "Height", "int", "Integer", "", 0);
                AddProperty(templateProps, "Path", "KString", "XRefUrl", "", "");
                AddProperty(templateProps, "StartFrame", "int", "Integer", "", 0);
                AddProperty(templateProps, "StopFrame", "int", "Integer", "", 0);
                AddProperty(templateProps, "PlaySpeed", "double", "Number", "", 0.0);
                AddProperty(templateProps, "Offset", "KTime", "Time", "", 0L);
                AddProperty(templateProps, "InterlaceMode", "enum", "", "", 0);
                AddProperty(templateProps, "FreeRunning", "bool", "", "", 0);
                AddProperty(templateProps, "Loop", "bool", "", "", 0);
                AddProperty(templateProps, "AccessMode", "enum", "", "", 0);
                break;

            case "FbxAnimStack":
                AddProperty(templateProps, "Description", "KString", "", "", "");
                AddProperty(templateProps, "LocalStart", "KTime", "Time", "", 0L);
                AddProperty(templateProps, "LocalStop", "KTime", "Time", "", 0L);
                AddProperty(templateProps, "ReferenceStart", "KTime", "Time", "", 0L);
                AddProperty(templateProps, "ReferenceStop", "KTime", "Time", "", 0L);
                break;

            case "FbxAnimLayer":
                AddProperty(templateProps, "Weight", "Number", "", "A", 100.0);
                AddProperty(templateProps, "Mute", "bool", "", "", 0);
                AddProperty(templateProps, "Solo", "bool", "", "", 0);
                AddProperty(templateProps, "Lock", "bool", "", "", 0);
                AddProperty(templateProps, "Color", "ColorRGB", "Color", "", 0.8, 0.8, 0.8);
                AddProperty(templateProps, "BlendMode", "enum", "", "", 0);
                AddProperty(templateProps, "RotationAccumulationMode", "enum", "", "", 0);
                AddProperty(templateProps, "ScaleAccumulationMode", "enum", "", "", 0);
                AddProperty(templateProps, "BlendModeBypass", "ULongLong", "", "", 0L);
                break;

            case "FbxAnimCurveNode":
                AddProperty(templateProps, "d", "Compound", "", "");
                break;

            case "FbxCamera":
                AddProperty(templateProps, "Position", "Vector", "", "A", 0.0, 0.0, -50.0);
                AddProperty(templateProps, "UpVector", "Vector", "", "A", 0.0, 1.0, 0.0);
                AddProperty(templateProps, "InterestPosition", "Vector", "", "A", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "Roll", "Roll", "", "A", 0.0);
                AddProperty(templateProps, "OpticalCenterX", "OpticalCenterX", "", "A", 0.0);
                AddProperty(templateProps, "OpticalCenterY", "OpticalCenterY", "", "A", 0.0);
                AddProperty(templateProps, "BackgroundColor", "Color", "", "A", 0.63, 0.63, 0.63);
                AddProperty(templateProps, "TurnTable", "Number", "", "A", 0.0);
                AddProperty(templateProps, "DisplayTurnTableIcon", "bool", "", "", 0);
                AddProperty(templateProps, "UseMotionBlur", "bool", "", "", 0);
                AddProperty(templateProps, "UseRealTimeMotionBlur", "bool", "", "", 1);
                AddProperty(templateProps, "Motion Blur Intensity", "Number", "", "A", 1.0);
                AddProperty(templateProps, "AspectRatioMode", "enum", "", "", 0);
                AddProperty(templateProps, "AspectWidth", "double", "Number", "", 320.0);
                AddProperty(templateProps, "AspectHeight", "double", "Number", "", 200.0);
                AddProperty(templateProps, "PixelAspectRatio", "double", "Number", "", 1.0);
                AddProperty(templateProps, "FilmOffsetX", "Number", "", "A", 0.0);
                AddProperty(templateProps, "FilmOffsetY", "Number", "", "A", 0.0);
                AddProperty(templateProps, "FilmWidth", "double", "Number", "", 0.816);
                AddProperty(templateProps, "FilmHeight", "double", "Number", "", 0.612);
                AddProperty(templateProps, "FilmAspectRatio", "double", "Number", "", 1.3333333333333333);
                AddProperty(templateProps, "FilmSqueezeRatio", "double", "Number", "", 1.0);
                AddProperty(templateProps, "FilmFormatIndex", "enum", "", "", 0);
                AddProperty(templateProps, "PreScale", "Number", "", "A", 1.0);
                AddProperty(templateProps, "FilmTranslateX", "Number", "", "A", 0.0);
                AddProperty(templateProps, "FilmTranslateY", "Number", "", "A", 0.0);
                AddProperty(templateProps, "FilmRollPivotX", "Number", "", "A", 0.0);
                AddProperty(templateProps, "FilmRollPivotY", "Number", "", "A", 0.0);
                AddProperty(templateProps, "FilmRollValue", "Number", "", "A", 0.0);
                AddProperty(templateProps, "FilmRollOrder", "enum", "", "", 0);
                AddProperty(templateProps, "ApertureMode", "enum", "", "", 2);
                AddProperty(templateProps, "GateFit", "enum", "", "", 0);
                AddProperty(templateProps, "FieldOfView", "FieldOfView", "", "A", 25.114999771118164);
                AddProperty(templateProps, "FieldOfViewX", "FieldOfView", "", "A", 40.0);
                AddProperty(templateProps, "FieldOfViewY", "FieldOfView", "", "A", 40.0);
                AddProperty(templateProps, "FocalLength", "Number", "", "A", 34.89327621672628);
                AddProperty(templateProps, "CameraFormat", "enum", "", "", 0);
                AddProperty(templateProps, "UseFrameColor", "bool", "", "", 0);
                AddProperty(templateProps, "FrameColor", "ColorRGB", "Color", "", 0.3, 0.3, 0.3);
                AddProperty(templateProps, "ShowName", "bool", "", "", 1);
                AddProperty(templateProps, "ShowInfoOnMoving", "bool", "", "", 1);
                AddProperty(templateProps, "ShowGrid", "bool", "", "", 1);
                AddProperty(templateProps, "ShowOpticalCenter", "bool", "", "", 0);
                AddProperty(templateProps, "ShowAzimut", "bool", "", "", 1);
                AddProperty(templateProps, "ShowTimeCode", "bool", "", "", 0);
                AddProperty(templateProps, "ShowAudio", "bool", "", "", 0);
                AddProperty(templateProps, "AudioColor", "Vector3D", "Vector", "", 0.0, 1.0, 0.0);
                AddProperty(templateProps, "NearPlane", "double", "Number", "", 10.0);
                AddProperty(templateProps, "FarPlane", "double", "Number", "", 4000.0);
                AddProperty(templateProps, "AutoComputeClipPanes", "bool", "", "", 0);
                AddProperty(templateProps, "ViewCameraToLookAt", "bool", "", "", 1);
                AddProperty(templateProps, "ViewFrustumNearFarPlane", "bool", "", "", 0);
                AddProperty(templateProps, "ViewFrustumBackPlaneMode", "enum", "", "", 2);
                AddProperty(templateProps, "BackPlaneDistance", "Number", "", "A", 4000.0);
                AddProperty(templateProps, "BackPlaneDistanceMode", "enum", "", "", 1);
                AddProperty(templateProps, "ViewFrustumFrontPlaneMode", "enum", "", "", 2);
                AddProperty(templateProps, "FrontPlaneDistance", "Number", "", "A", 10.0);
                AddProperty(templateProps, "FrontPlaneDistanceMode", "enum", "", "", 1);
                AddProperty(templateProps, "LockMode", "bool", "", "", 0);
                AddProperty(templateProps, "LockInterestNavigation", "bool", "", "", 0);
                AddProperty(templateProps, "FitImage", "bool", "", "", 0);
                AddProperty(templateProps, "Crop", "bool", "", "", 0);
                AddProperty(templateProps, "Center", "bool", "", "", 1);
                AddProperty(templateProps, "KeepRatio", "bool", "", "", 1);
                AddProperty(templateProps, "BackgroundAlphaTreshold", "double", "Number", "", 0.5);
                AddProperty(templateProps, "ShowBackplate", "bool", "", "", 1);
                AddProperty(templateProps, "BackPlaneOffset", "Vector2D", "Vector", "", 0.0, 0.0);
                AddProperty(templateProps, "BackPlaneRotation", "double", "Number", "", 0.0);
                AddProperty(templateProps, "BackPlaneScaling", "Vector2D", "Vector", "", 1.0, 1.0);
                AddProperty(templateProps, "ShowFrontplate", "bool", "", "", 1);
                AddProperty(templateProps, "FrontPlateFitImage", "bool", "", "", 1);
                AddProperty(templateProps, "FrontPlateCrop", "bool", "", "", 0);
                AddProperty(templateProps, "FrontPlateCenter", "bool", "", "", 1);
                AddProperty(templateProps, "FrontPlateKeepRatio", "bool", "", "", 1);
                AddProperty(templateProps, "ForegroundOpacity", "double", "Number", "", 0.5);
                AddProperty(templateProps, "FrontPlaneOffset", "Vector2D", "Vector", "", 0.0, 0.0);
                AddProperty(templateProps, "FrontPlaneRotation", "double", "Number", "", 0.0);
                AddProperty(templateProps, "FrontPlaneScaling", "Vector2D", "Vector", "", 1.0, 1.0);
                AddProperty(templateProps, "BackgroundMode", "enum", "", "", 0);
                AddProperty(templateProps, "ForegroundTransparent", "bool", "", "", 0);
                AddProperty(templateProps, "DisplaySafeArea", "bool", "", "", 0);
                AddProperty(templateProps, "DisplaySafeAreaOnRender", "bool", "", "", 0);
                AddProperty(templateProps, "SafeAreaDisplayStyle", "enum", "", "", 1);
                AddProperty(templateProps, "SafeAreaAspectRatio", "double", "Number", "", 1.3333333333333333);
                AddProperty(templateProps, "Use2DMagnifierZoom", "bool", "", "", 0);
                AddProperty(templateProps, "2DMagnifierZoom", "Number", "", "A", 100.0);
                AddProperty(templateProps, "2DMagnifierX", "Number", "", "A", 50.0);
                AddProperty(templateProps, "2DMagnifierY", "Number", "", "A", 50.0);
                AddProperty(templateProps, "CameraProjectionType", "enum", "", "", 0);
                AddProperty(templateProps, "OrthoZoom", "double", "Number", "", 1.0);
                AddProperty(templateProps, "UseRealTimeDOFAndAA", "bool", "", "", 0);
                AddProperty(templateProps, "UseDepthOfField", "bool", "", "", 0);
                AddProperty(templateProps, "FocusSource", "enum", "", "", 0);
                AddProperty(templateProps, "FocusAngle", "double", "Number", "", 3.5);
                AddProperty(templateProps, "FocusDistance", "double", "Number", "", 200.0);
                AddProperty(templateProps, "UseAntialiasing", "bool", "", "", 0);
                AddProperty(templateProps, "AntialiasingIntensity", "double", "Number", "", 0.77777);
                AddProperty(templateProps, "AntialiasingMethod", "enum", "", "", 0);
                AddProperty(templateProps, "UseAccumulationBuffer", "bool", "", "", 0);
                AddProperty(templateProps, "FrameSamplingCount", "int", "Integer", "", 7);
                AddProperty(templateProps, "FrameSamplingType", "enum", "", "", 1);
                break;

            case "FbxLight":
                AddProperty(templateProps, "LightType", "enum", "", "", 0);
                AddProperty(templateProps, "CastLight", "bool", "", "", 1);
                AddProperty(templateProps, "DrawVolumetricLight", "bool", "", "", 1);
                AddProperty(templateProps, "DrawGroundProjection", "bool", "", "", 1);
                AddProperty(templateProps, "DrawFrontFacingVolumetricLight", "bool", "", "", 0);
                AddProperty(templateProps, "Color", "Color", "", "A", 1.0, 1.0, 1.0);
                AddProperty(templateProps, "Intensity", "Number", "", "A", 100.0);
                AddProperty(templateProps, "InnerAngle", "Number", "", "A", 0.0);
                AddProperty(templateProps, "OuterAngle", "Number", "", "A", 45.0);
                AddProperty(templateProps, "Fog", "Number", "", "A", 50.0);
                AddProperty(templateProps, "DecayType", "enum", "", "", 2);
                AddProperty(templateProps, "DecayStart", "double", "Number", "", 0.0);
                AddProperty(templateProps, "FileName", "KString", "", "", "");
                AddProperty(templateProps, "EnableNearAttenuation", "bool", "", "", 0);
                AddProperty(templateProps, "NearAttenuationStart", "double", "Number", "", 0.0);
                AddProperty(templateProps, "NearAttenuationEnd", "double", "Number", "", 0.0);
                AddProperty(templateProps, "EnableFarAttenuation", "bool", "", "", 0);
                AddProperty(templateProps, "FarAttenuationStart", "double", "Number", "", 0.0);
                AddProperty(templateProps, "FarAttenuationEnd", "double", "Number", "", 0.0);
                AddProperty(templateProps, "CastShadows", "bool", "", "", 0);
                AddProperty(templateProps, "ShadowColor", "Color", "", "A", 0.0, 0.0, 0.0);
                AddProperty(templateProps, "AreaLightShape", "enum", "", "", 0);
                AddProperty(templateProps, "LeftBarnDoor", "float", "", "A", 20.0);
                AddProperty(templateProps, "RightBarnDoor", "float", "", "A", 20.0);
                AddProperty(templateProps, "TopBarnDoor", "float", "", "A", 20.0);
                AddProperty(templateProps, "BottomBarnDoor", "float", "", "A", 20.0);
                AddProperty(templateProps, "EnableBarnDoor", "bool", "", "", 0);
                break;

            case "FbxNull":
                AddProperty(templateProps, "Color", "ColorRGB", "Color", "", 0.8, 0.8, 0.8);
                AddProperty(templateProps, "Size", "double", "Number", "", 100.0);
                AddProperty(templateProps, "Look", "enum", "", "", 1);
                break;
        }
    }

    private string GetPropertyTemplateName(string fbxClassName, HashSet<string> subTypes)
    {
        switch (fbxClassName)
        {
            case "Model":
                return "FbxNode";
            case "Geometry":
                if (subTypes.Contains("NurbsCurve"))
                    return "FbxNurbsCurve";
                if (subTypes.Contains("NurbsSurface"))
                    return "FbxNurbsSurface";
                if (subTypes.Contains("Shape"))
                    return "FbxShape";
                return "FbxMesh";
            case "Material":
                return "FbxSurfacePhong";
            case "Texture":
                return "FbxFileTexture";
            case "Video":
                return "FbxVideo";
            case "NodeAttribute":
                if (subTypes.Contains("Camera"))
                    return "FbxCamera";
                if (subTypes.Contains("Light"))
                    return "FbxLight";
                return "FbxNull";
            case "AnimationStack":
                return "FbxAnimStack";
            case "AnimationLayer":
                return "FbxAnimLayer";
            case "AnimationCurveNode":
                return "FbxAnimCurveNode";
            case "AnimationCurve":
                return null;
            case "Deformer":
                return null;
            case "Pose":
                return null;
            case "CollectionExclusive":
                return null;
            case "LayeredTexture":
                return null;
            default:
                return null;
        }
    }
    private void BuildConnections()
    {
        var connectionsNode = new FbxNode() { Name = "Connections" };

        foreach (var conn in connections)
        {
            if (string.IsNullOrEmpty(conn.property))
            {
                connectionsNode.Nodes.Add(new FbxNode()
                {
                    Name = "C",
                    Properties = { conn.type, conn.childId, conn.parentId }
                });
            }
            else
            {
                connectionsNode.Nodes.Add(new FbxNode()
                {
                    Name = "C",
                    Properties = { conn.type, conn.childId, conn.parentId, conn.property }
                });
            }
        }

        doc.Nodes.Add(connectionsNode);
    }

    private void BuildTakes()
    {
        var takes = new FbxNode() { Name = "Takes" };
        takes.Nodes.Add(new FbxNode() { Name = "Current", Properties = { "" } });
        doc.Nodes.Add(takes);
    }

    private FbxNode GetObjectsNode()
    {
        return doc.Nodes.Find(n => n.Name == "Objects");
    }

    private long GetOrCreateId(object source)
    {
        if (source != null && objectIds.ContainsKey(source))
            return objectIds[source];

        long id = nextId++;
        if (source != null)
            objectIds[source] = id;

        return id;
    }

    private FbxNode CreateProperties70()
    {
        return new FbxNode() { Name = "Properties70" };
    }

    private void AddProperty(FbxNode props, string name, string type1, string type2, string flags, params object[] values)
    {
        var prop = new FbxNode() { Name = "P" };
        prop.Properties.Add(name);
        prop.Properties.Add(type1);
        prop.Properties.Add(type2);
        prop.Properties.Add(flags);

        foreach (var val in values)
        {
            if (val is bool boolVal)
                prop.Properties.Add(boolVal ? 1 : 0);
            else if (val is float floatVal)
                prop.Properties.Add((double)floatVal);
            else
                prop.Properties.Add(val);
        }

        props.Nodes.Add(prop);
    }
    private void AddLayerElementNormal(FbxNode geometry, Vector3[] normals, int[] triangles, int index)
    {
        var layerElement = new FbxNode() { Name = "LayerElementNormal", Properties = { index } };
        layerElement.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 101 } });
        layerElement.Nodes.Add(new FbxNode() { Name = "Name", Properties = { "" } });
        layerElement.Nodes.Add(new FbxNode() { Name = "MappingInformationType", Properties = { "ByPolygonVertex" } });
        layerElement.Nodes.Add(new FbxNode() { Name = "ReferenceInformationType", Properties = { "Direct" } });

        var normalArray = new double[triangles.Length * 3];
        for (int i = 0; i < triangles.Length; i++)
        {
            Vector3 n = normals[triangles[i]];
            normalArray[i * 3 + 0] = n.x;
            normalArray[i * 3 + 1] = n.y;
            normalArray[i * 3 + 2] = n.z;
        }

        layerElement.Nodes.Add(new FbxNode() { Name = "Normals", Properties = { normalArray } });

        geometry.Nodes.Add(layerElement);
    }

    private void AddLayerElementBinormal(FbxNode geometry, Vector3[] binormals, int[] triangles, int index)
    {
        var layerElement = new FbxNode() { Name = "LayerElementBinormal", Properties = { index } };
        layerElement.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 101 } });
        layerElement.Nodes.Add(new FbxNode() { Name = "Name", Properties = { "" } });
        layerElement.Nodes.Add(new FbxNode() { Name = "MappingInformationType", Properties = { "ByPolygonVertex" } });
        layerElement.Nodes.Add(new FbxNode() { Name = "ReferenceInformationType", Properties = { "Direct" } });

        var binormalArray = new double[triangles.Length * 3];
        for (int i = 0; i < triangles.Length; i++)
        {
            Vector3 b = binormals[triangles[i]];
            binormalArray[i * 3 + 0] = b.x;
            binormalArray[i * 3 + 1] = b.y;
            binormalArray[i * 3 + 2] = b.z;
        }

        layerElement.Nodes.Add(new FbxNode() { Name = "Binormals", Properties = { binormalArray } });

        geometry.Nodes.Add(layerElement);
    }

    private void AddLayerElementTangent(FbxNode geometry, Vector4[] tangents, int[] triangles, int index)
    {
        var layerElement = new FbxNode() { Name = "LayerElementTangent", Properties = { index } };
        layerElement.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 101 } });
        layerElement.Nodes.Add(new FbxNode() { Name = "Name", Properties = { "" } });
        layerElement.Nodes.Add(new FbxNode() { Name = "MappingInformationType", Properties = { "ByPolygonVertex" } });
        layerElement.Nodes.Add(new FbxNode() { Name = "ReferenceInformationType", Properties = { "Direct" } });

        var tangentArray = new double[triangles.Length * 3];
        for (int i = 0; i < triangles.Length; i++)
        {
            Vector4 t = tangents[triangles[i]];
            tangentArray[i * 3 + 0] = t.x;
            tangentArray[i * 3 + 1] = t.y;
            tangentArray[i * 3 + 2] = t.z;
        }

        layerElement.Nodes.Add(new FbxNode() { Name = "Tangents", Properties = { tangentArray } });

        geometry.Nodes.Add(layerElement);
    }

    private void AddLayerElementUV(FbxNode geometry, Vector2[] uvs, int[] triangles, int uvIndex)
    {
        var layerElement = new FbxNode() { Name = "LayerElementUV", Properties = { uvIndex } };
        layerElement.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 101 } });
        layerElement.Nodes.Add(new FbxNode() { Name = "Name", Properties = { "UVChannel_" + (uvIndex + 1) } });
        layerElement.Nodes.Add(new FbxNode() { Name = "MappingInformationType", Properties = { "ByPolygonVertex" } });
        layerElement.Nodes.Add(new FbxNode() { Name = "ReferenceInformationType", Properties = { "IndexToDirect" } });

        var uvArray = new double[uvs.Length * 2];
        for (int i = 0; i < uvs.Length; i++)
        {
            uvArray[i * 2 + 0] = uvs[i].x;
            uvArray[i * 2 + 1] = uvs[i].y;
        }

        layerElement.Nodes.Add(new FbxNode() { Name = "UV", Properties = { uvArray } });
        layerElement.Nodes.Add(new FbxNode() { Name = "UVIndex", Properties = { triangles } });

        geometry.Nodes.Add(layerElement);
    }

    private void AddLayerElementColor(FbxNode geometry, Color[] colors, int[] triangles, int colorIndex)
    {
        var layerElement = new FbxNode() { Name = "LayerElementColor", Properties = { colorIndex } };
        layerElement.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 101 } });
        layerElement.Nodes.Add(new FbxNode() { Name = "Name", Properties = { colorIndex == 0 ? "" : "ColorChannel_" + (colorIndex + 1) } });
        layerElement.Nodes.Add(new FbxNode() { Name = "MappingInformationType", Properties = { "ByPolygonVertex" } });
        layerElement.Nodes.Add(new FbxNode() { Name = "ReferenceInformationType", Properties = { "IndexToDirect" } });

        var colorArray = new double[colors.Length * 4];
        for (int i = 0; i < colors.Length; i++)
        {
            colorArray[i * 4 + 0] = colors[i].r;
            colorArray[i * 4 + 1] = colors[i].g;
            colorArray[i * 4 + 2] = colors[i].b;
            colorArray[i * 4 + 3] = colors[i].a;
        }

        layerElement.Nodes.Add(new FbxNode() { Name = "Colors", Properties = { colorArray } });
        layerElement.Nodes.Add(new FbxNode() { Name = "ColorIndex", Properties = { triangles } });

        geometry.Nodes.Add(layerElement);
    }

    private void AddLayerElementMaterial(FbxNode geometry, int[] materialIndices)
    {
        var layerElement = new FbxNode() { Name = "LayerElementMaterial", Properties = { 0 } };
        layerElement.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 101 } });
        layerElement.Nodes.Add(new FbxNode() { Name = "Name", Properties = { "" } });
        layerElement.Nodes.Add(new FbxNode() { Name = "MappingInformationType", Properties = { "ByPolygon" } });
        layerElement.Nodes.Add(new FbxNode() { Name = "ReferenceInformationType", Properties = { "IndexToDirect" } });

        layerElement.Nodes.Add(new FbxNode() { Name = "Materials", Properties = { materialIndices } });

        geometry.Nodes.Add(layerElement);
    }

    private void AddLayerElementSmoothing(FbxNode geometry, int[] smoothing)
    {
        var layerElement = new FbxNode() { Name = "LayerElementSmoothing", Properties = { 0 } };
        layerElement.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 102 } });
        layerElement.Nodes.Add(new FbxNode() { Name = "Name", Properties = { "" } });
        layerElement.Nodes.Add(new FbxNode() { Name = "MappingInformationType", Properties = { "ByPolygon" } });
        layerElement.Nodes.Add(new FbxNode() { Name = "ReferenceInformationType", Properties = { "Direct" } });

        layerElement.Nodes.Add(new FbxNode() { Name = "Smoothing", Properties = { smoothing } });

        geometry.Nodes.Add(layerElement);
    }

    private void AddLayerElementCrease(FbxNode geometry, double[] crease, string type)
    {
        var layerElement = new FbxNode() { Name = "LayerElementCrease", Properties = { 0 } };
        layerElement.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 101 } });
        layerElement.Nodes.Add(new FbxNode() { Name = "Name", Properties = { type } });
        layerElement.Nodes.Add(new FbxNode() { Name = "MappingInformationType", Properties = { type == "EdgeCrease" ? "ByEdge" : "ByVertex" } });
        layerElement.Nodes.Add(new FbxNode() { Name = "ReferenceInformationType", Properties = { "Direct" } });

        layerElement.Nodes.Add(new FbxNode() { Name = type, Properties = { crease } });

        geometry.Nodes.Add(layerElement);
    }

    private void AddLayerElementHole(FbxNode geometry, int[] holes)
    {
        var layerElement = new FbxNode() { Name = "LayerElementHole", Properties = { 0 } };
        layerElement.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 101 } });
        layerElement.Nodes.Add(new FbxNode() { Name = "Name", Properties = { "" } });
        layerElement.Nodes.Add(new FbxNode() { Name = "MappingInformationType", Properties = { "ByPolygon" } });
        layerElement.Nodes.Add(new FbxNode() { Name = "ReferenceInformationType", Properties = { "Direct" } });

        layerElement.Nodes.Add(new FbxNode() { Name = "Hole", Properties = { holes } });

        geometry.Nodes.Add(layerElement);
    }

    private void AddLayerElementUserData(FbxNode geometry, FbxUserDataLayer userData, int index)
    {
        var layerElement = new FbxNode() { Name = "LayerElementUserData", Properties = { index } };
        layerElement.Nodes.Add(new FbxNode() { Name = "Version", Properties = { 101 } });
        layerElement.Nodes.Add(new FbxNode() { Name = "Name", Properties = { userData.name } });
        layerElement.Nodes.Add(new FbxNode() { Name = "MappingInformationType", Properties = { userData.mappingType } });
        layerElement.Nodes.Add(new FbxNode() { Name = "ReferenceInformationType", Properties = { userData.referenceType } });

        layerElement.Nodes.Add(new FbxNode() { Name = "UserData", Properties = { userData.data } });

        if (userData.indices != null)
            layerElement.Nodes.Add(new FbxNode() { Name = "UserDataIndex", Properties = { userData.indices } });

        geometry.Nodes.Add(layerElement);
    }

    private double[] MatrixToArray(Matrix4x4 matrix)
    {
        return new double[]
        {
            matrix.m00, matrix.m01, matrix.m02, matrix.m03,
            matrix.m10, matrix.m11, matrix.m12, matrix.m13,
            matrix.m20, matrix.m21, matrix.m22, matrix.m23,
            matrix.m30, matrix.m31, matrix.m32, matrix.m33
        };
    }

    #endregion

    public class FbxModelData
    {
        public object source;
        public string name;
        public string modelType = "Null";

        public Vector3 localPosition;
        public Vector3 localRotation;
        public Vector3 localScale;

        public Vector3? preRotation;
        public Vector3? postRotation;
        public Vector3? rotationOffset;
        public Vector3? rotationPivot;
        public Vector3? scalingOffset;
        public Vector3? scalingPivot;
        public FbxRotationOrder? rotationOrder;

        public long? parentId;

        public bool? visibility;
        public bool? visibilityInheritance;

        public bool? show;
        public bool? freeze;
        public bool? lodBox;

        public int shading = 1;
        public string culling = "CullingOff";

        public Dictionary<string, FbxProperty> customProperties;
    }

    public enum FbxRotationOrder
    {
        XYZ = 0,
        XZY = 1,
        YZX = 2,
        YXZ = 3,
        ZXY = 4,
        ZYX = 5,
        SphericXYZ = 6
    }

    public struct FbxProperty
    {
        public string type;
        public string subType;
        public string flags;
        public object[] values;
    }

    public class FbxGeometryData
    {
        public object source;
        public string name;

        public Vector3[] vertices;
        public int[] triangles;
        public int[] edges;

        public Vector3[] normals;
        public Vector3[] binormals;
        public Vector4[] tangents;

        public Vector2[][] uvChannels;

        public Color[][] colorChannels;

        public int[] materialIndices;

        public int[] smoothing;
        public double[] edgeCrease;
        public double[] vertexCrease;
        public int[] holes;
        public FbxUserDataLayer[] userDataLayers;
    }

    public class FbxUserDataLayer
    {
        public string name;
        public string mappingType;
        public string referenceType;
        public object data;
        public int[] indices;
    }

    public class FbxNurbsCurveData
    {
        public object source;
        public string name;
        public int order;
        public int dimension;
        public bool isClosed;
        public bool isRational;
        public Vector3[] controlPoints;
        public double[] weights;
        public double[] knots;
    }

    public class FbxNurbsSurfaceData
    {
        public object source;
        public string name;
        public int orderU;
        public int orderV;
        public bool isClosedU;
        public bool isClosedV;
        public bool isRational;
        public Vector3[] controlPoints;
        public double[] weights;
        public double[] knotsU;
        public double[] knotsV;
    }

    public class FbxMaterialData
    {
        public object source;
        public string name;
        public string shadingModel = "phong";
        public Color? diffuseColor;
        public double? diffuseFactor;

        public Color? specularColor;
        public double? specularFactor;

        public Color? emissiveColor;
        public double? emissiveFactor;

        public Color? ambientColor;
        public double? ambientFactor;

        public Color? transparentColor;
        public double? transparencyFactor;

        public double? opacity;
        public double? shininess;
        public double? reflectivity;

        public Color? reflectionColor;
        public double? reflectionFactor;

        public double? metallic;
        public double? roughness;
        public double? normalScale;

        public double? bumpFactor;
        public double? displacementFactor;

        public Dictionary<string, FbxProperty> customProperties;
    }

    public class FbxTextureData
    {
        public object source;
        public string name;
        public string filePath;
        public string relativeFilePath;

        public string uvSet;
        public Vector2? translation;
        public Vector2? scale;
        public double? rotation;

        public FbxWrapMode? wrapModeU;
        public FbxWrapMode? wrapModeV;

        public FbxTextureBlendMode? blendMode;
        public FbxTextureAlphaSource? alphaSource;

        public Vector4? cropping;
    }

    public enum FbxWrapMode
    {
        Repeat = 0,
        Clamp = 1
    }

    public enum FbxTextureBlendMode
    {
        Translucent = 0,
        Additive = 1,
        Modulate = 2,
        Modulate2 = 3,
        Over = 4,
        Normal = 5,
        Dissolve = 6,
        Darken = 7,
        ColorBurn = 8,
        LinearBurn = 9,
        DarkerColor = 10,
        Lighten = 11,
        Screen = 12,
        ColorDodge = 13,
        LinearDodge = 14,
        LighterColor = 15,
        SoftLight = 16,
        HardLight = 17,
        VividLight = 18,
        LinearLight = 19,
        PinLight = 20,
        HardMix = 21,
        Difference = 22,
        Exclusion = 23,
        Subtract = 24,
        Divide = 25,
        Hue = 26,
        Saturation = 27,
        Color = 28,
        Luminosity = 29,
        Overlay = 30,
        BlendModeCount = 31
    }

    public enum FbxTextureAlphaSource
    {
        None = 0,
        RGBIntensity = 1,
        Black = 2
    }

    public class FbxLayeredTextureData
    {
        public object source;
        public string name;
        public FbxTextureBlendMode blendMode;
        public double alpha;
    }

    public class FbxVideoData
    {
        public object source;
        public string name;
        public string filePath;
        public string relativeFilePath;
        public byte[] content;
        public int? width;
        public int? height;
    }

    public class FbxCameraData
    {
        public object source;
        public string name;

        public Vector3 position;
        public Vector3 upVector;
        public Vector3 targetPosition;

        public double fieldOfView;
        public double focalLength;
        public double nearClipPlane;
        public double farClipPlane;
        public bool isOrthographic;
        public double orthographicSize;
        public double aspectRatio;

        public double? filmWidth;
        public double? filmHeight;
        public double? filmAspectRatio;
        public FbxCameraApertureMode? apertureMode;
        public FbxCameraGateFit? gateFit;

        public double? fStop;
        public double? focusDistance;

        public Color? backgroundColor;
    }

    public enum FbxCameraApertureMode
    {
        HorizAndVert = 0,
        Horizontal = 1,
        Vertical = 2,
        FocalLength = 3
    }

    public enum FbxCameraGateFit
    {
        None = 0,
        Vertical = 1,
        Horizontal = 2,
        Fill = 3,
        Overscan = 4,
        Stretch = 5
    }

    public class FbxCameraSwitcherData
    {
        public object source;
        public string name;
        public int cameraIndex;
    }

    public class FbxLightData
    {
        public object source;
        public string name;
        public LightType type;

        public Color color;
        public float intensity;

        public float spotAngle;
        public float? innerConeAngle;

        public float range;

        public bool castLight = true;
        public bool castShadows = true;
        public Color? shadowColor;

        public FbxLightDecayType decayType = FbxLightDecayType.QuadraticDecay;

        public FbxAreaLightShape? areaLightShape;

        public double? nearAttenuationStart;
        public double? nearAttenuationEnd;
        public double? farAttenuationStart;
        public double? farAttenuationEnd;
        public bool? enableNearAttenuation;
        public bool? enableFarAttenuation;
    }

    public enum FbxLightDecayType
    {
        None = 0,
        Linear = 1,
        QuadraticDecay = 2,
        Cubic = 3
    }

    public enum FbxAreaLightShape
    {
        Rectangle = 0,
        Sphere = 1,
        Disc = 2,
        Box = 3,
        Cylinder = 4
    }

    public class FbxSkinData
    {
        public object source;
        public string name;
        public double deformAccuracy = 50.0;
        public string skinningType = "Linear";
        public FbxClusterData[] clusters;
    }

    public class FbxClusterData
    {
        public string boneName;
        public long? boneId;
        public int[] indices;
        public double[] weights;
        public Matrix4x4 transform;
        public Matrix4x4 transformLink;
        public Matrix4x4? transformAssociateModel;
    }

    public class FbxBlendShapeData
    {
        public object source;
        public string name;
        public FbxBlendShapeChannelData[] channels;
    }

    public class FbxBlendShapeChannelData
    {
        public string name;
        public double deformPercent;
        public FbxShapeData[] targetShapes;
    }

    public class FbxShapeData
    {
        public string name;
        public int[] indices;
        public Vector3[] deltaVertices;
        public Vector3[] deltaNormals;
        public double fullWeight = 100.0;
    }

    public class FbxVertexCacheData
    {
        public object source;
        public string name;
        public string channel = "points";
        public string cacheType = "PC2";
        public string cacheFile;
        public string cacheName;
    }

    public class FbxAnimationStackData
    {
        public object source;
        public string name;
        public long localStart;
        public long localStop;
        public long referenceStart;
        public long referenceStop;
    }

    public class FbxAnimationLayerData
    {
        public object source;
        public string name;
        public double? weight;
        public bool? mute;
        public bool? solo;
        public bool? _lock;
        public FbxAnimationLayerBlendMode? blendMode;
        public FbxAnimationLayerRotationBlendMode? rotationBlendMode;
        public FbxAnimationLayerScaleBlendMode? scaleBlendMode;
    }

    public enum FbxAnimationLayerBlendMode
    {
        Additive = 0,
        Override = 1,
        OverridePassthrough = 2
    }

    public enum FbxAnimationLayerRotationBlendMode
    {
        ByLayer = 0,
        ByChannel = 1
    }

    public enum FbxAnimationLayerScaleBlendMode
    {
        Multiply = 0,
        Additive = 1
    }

    public class FbxAnimationCurveNodeData
    {
        public object source;
        public string propertyName;
        public double[] defaultValues;
    }

    public class FbxAnimationCurveData
    {
        public object source;
        public FbxKeyframe[] keyframes;
        public float defaultValue;
    }

    public struct FbxKeyframe
    {
        public double time;
        public float value;
        public FbxInterpolationType interpolation;
        public float rightSlope;
        public float nextLeftSlope;
        public float rightWeight;
        public float nextLeftWeight;
    }

    public enum FbxInterpolationType
    {
        Constant = 0x00000002,
        Linear = 0x00000004,
        Cubic = 0x00000010
    }

    public class FbxConstraintData
    {
        public object source;
        public string name;
        public bool active = true;
        public double weight = 100.0;
        public bool? _lock;

        public bool? constrainTranslationX;
        public bool? constrainTranslationY;
        public bool? constrainTranslationZ;
        public bool? constrainRotationX;
        public bool? constrainRotationY;
        public bool? constrainRotationZ;
    }

    public class FbxPoseData
    {
        public object source;
        public string name = "BindPose";
        public bool isBindPose = true;
        public FbxPoseNodeData[] nodes;
    }

    public struct FbxPoseNodeData
    {
        public long nodeId;
        public Matrix4x4 matrix;
    }

    public class FbxCollectionData
    {
        public object source;
        public string name;
    }

    public class FbxDisplayLayerData
    {
        public object source;
        public string name;
        public Color? color;
        public bool show = true;
        public bool freeze = false;
        public bool lodBox = false;
    }

    public class FbxSelectionNodeData
    {
        public object source;
        public string name;
    }

    public class FbxSelectionSetData
    {
        public object source;
        public string name;
    }

    public class FbxSceneInfoData
    {
        public string documentUrl;
        public string srcDocumentUrl;
        public string title;
        public string subject;
        public string author;
        public string keywords;
        public string revision;
        public string comment;
    }

    public class FbxAudioData
    {
        public object source;
        public string name;
        public string filePath;
        public string relativeFilePath;
        public byte[] content;
    }

    public class FbxCharacterData
    {
        public object source;
        public string name;
        public FbxCharacterScaleCompensationMode? scaleCompensationMode;
    }

    public enum FbxCharacterScaleCompensationMode
    {
        None = 0,
        Standard = 1
    }

    public class FbxControlSetData
    {
        public object source;
        public string name;
    }

    public class FbxImplementationData
    {
        public object source;
        public string name;
        public string language = "HLSL";
        public string languageVersion = "1.0";
        public string renderAPI;
        public string renderAPIVersion;
    }

    public class FbxBindingTableData
    {
        public object source;
        public string name;
        public FbxBindingTableEntry[] entries;
    }

    public struct FbxBindingTableEntry
    {
        public string source;
        public string destination;
    }

    public class FbxEmbeddedDataData
    {
        public object source;
        public string name;
        public string originalFileName;
        public byte[] content;
    }

    internal class FbxConnection
    {
        public string type;
        public long childId;
        public long parentId;
        public string property;
        public FbxConnection(string type, long childId, long parentId, string property = "")
        {
            this.type = type;
            this.childId = childId;
            this.parentId = parentId;
            this.property = property;
        }
    }
}