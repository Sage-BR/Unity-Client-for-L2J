using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class L2ActorMaterialVariantRepair
{
    struct PropertyValue
    {
        public int id;
        public ShaderPropertyType type;
        public float number;
        public int integer;
        public Vector4 vector;
        public Color color;
        public Texture texture;
        public Vector2 scale;
        public Vector2 offset;
    }

    [MenuItem("L2/Tools/Repair Actor Material Variants")]
    public static void Repair()
    {
        int repaired = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Resources/Data/SysTextures" }))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (material == null || material.parent == null || material.shader == null ||
                material.shader.name != "L2/World/ActorModulate2X") continue;

            var values = new List<PropertyValue>();
            Shader shader = material.shader;
            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                var value = new PropertyValue { id = shader.GetPropertyNameId(i), type = shader.GetPropertyType(i) };
                switch (value.type)
                {
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range: value.number = material.GetFloat(value.id); break;
                    case ShaderPropertyType.Int: value.integer = material.GetInteger(value.id); break;
                    case ShaderPropertyType.Color: value.color = material.GetColor(value.id); break;
                    case ShaderPropertyType.Vector: value.vector = material.GetVector(value.id); break;
                    case ShaderPropertyType.Texture:
                        value.texture = material.GetTexture(value.id);
                        value.scale = material.GetTextureScale(value.id);
                        value.offset = material.GetTextureOffset(value.id);
                        break;
                }
                values.Add(value);
            }

            int queue = material.renderQueue;
            bool instancing = material.enableInstancing;
            bool doubleSided = material.doubleSidedGI;
            var flags = material.globalIlluminationFlags;
            Undo.RecordObject(material, "Repair L2 Actor Material Variant");
            material.parent = null;
            foreach (var value in values)
            {
                switch (value.type)
                {
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range: material.SetFloat(value.id, value.number); break;
                    case ShaderPropertyType.Int: material.SetInteger(value.id, value.integer); break;
                    case ShaderPropertyType.Color: material.SetColor(value.id, value.color); break;
                    case ShaderPropertyType.Vector: material.SetVector(value.id, value.vector); break;
                    case ShaderPropertyType.Texture:
                        material.SetTexture(value.id, value.texture);
                        material.SetTextureScale(value.id, value.scale);
                        material.SetTextureOffset(value.id, value.offset);
                        break;
                }
            }
            material.renderQueue = queue;
            material.enableInstancing = instancing;
            material.doubleSidedGI = doubleSided;
            material.globalIlluminationFlags = flags;
            EditorUtility.SetDirty(material);
            repaired++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[L2] Materiais ActorModulate2X desvinculados de variantes: " + repaired);
    }
}
