using System.Collections.Generic;
using UnityEngine;

namespace Osteon2D
{
    /// <summary>
    /// Everything a skeleton needs at run time: the skeleton JSON, the atlas text, the atlas page textures and one material per
    /// (page, blend mode). Create it with Assets > Create > Osteon2D > Skeleton Data from an .osteon / .json file.
    /// </summary>
    [CreateAssetMenu(menuName = "Osteon2D/Skeleton Data", fileName = "SkeletonData")]
    public class OsteonSkeletonDataAsset : ScriptableObject
    {
        [Tooltip("Skeleton JSON exported from Osteon2D (File > Export > JSON), or the .osteon project itself.")]
        public TextAsset skeletonJson;
        [Tooltip("libGDX / Spine style .atlas text written by Osteon2D's texture packer.")]
        public TextAsset atlasText;
        [Tooltip("The atlas page images, in page order.")]
        public Texture2D[] pageTextures;
        [Tooltip("Pixels-to-units factor. 0.01 means 100 skeleton units are 1 Unity unit.")]
        public float scale = 0.01f;
        [Tooltip("Default crossfade duration between animations on a track, in seconds.")]
        public float defaultMix = 0.2f;
        [Tooltip("Optional: one material per page and blend mode, in the order page0 Normal, Additive, Multiply, Screen, page1 ... Leave empty to create them at run time.")]
        public Material[] materials;
        [Tooltip("Shader for materials made at run time. Filled in automatically. Referencing it here makes Unity include it in game builds (Shader.Find alone does not).")]
        public Shader shader;
        [Tooltip("Shader for tint black (two colour) slots. Filled in automatically, and referenced here so builds include it.")]
        public Shader tintBlackShader;

        SkeletonData data;
        Atlas atlas;
        bool warnedShader;
        readonly Dictionary<int, Material> runtimeMaterials = new Dictionary<int, Material>();

        public const string DefaultShaderName = "Osteon2D/Default";
        public const string TintBlackShaderName = "Osteon2D/Tint Black";

#if UNITY_EDITOR
        void OnValidate()
        {
            // Older assets, and assets made with Create > Osteon2D > Skeleton Data, get their shader references here.
            if (shader == null) shader = Shader.Find(DefaultShaderName);
            if (tintBlackShader == null) tintBlackShader = Shader.Find(TintBlackShaderName);
        }
#endif

        public SkeletonData GetSkeletonData(bool forceReload = false)
        {
            if (data == null || forceReload)
            {
                if (skeletonJson == null) throw new System.InvalidOperationException("OsteonSkeletonDataAsset '" + name + "' has no skeleton JSON.");
                data = SkeletonData.FromJson(skeletonJson.text);
                foreach (var w in data.warnings) Debug.LogWarning("[Osteon2D] " + name + ": " + w);
            }
            return data;
        }

        public Atlas GetAtlas(bool forceReload = false)
        {
            if (atlas == null || forceReload)
            {
                if (atlasText == null) throw new System.InvalidOperationException("OsteonSkeletonDataAsset '" + name + "' has no atlas text.");
                atlas = Atlas.Parse(atlasText.text);
            }
            return atlas;
        }

        /// <summary>The material for an atlas page and blend mode; created on demand when none was assigned.</summary>
        public Material GetMaterial(int page, BlendMode blend, bool tintBlack)
        {
            int slot = page * 4 + (int)blend;
            if (!tintBlack && materials != null && slot < materials.Length && materials[slot] != null) return materials[slot];
            int key = slot * 2 + (tintBlack ? 1 : 0);
            Material m;
            if (runtimeMaterials.TryGetValue(key, out m) && m != null) return m;
            // The referenced shader first: in a game build, Shader.Find only finds shaders that something references.
            Shader s = tintBlack ? tintBlackShader : shader;
            if (s == null) s = Shader.Find(tintBlack ? TintBlackShaderName : DefaultShaderName);
            if (s == null)
            {
                s = Shader.Find("Sprites/Default");
                if (!warnedShader)
                {
                    warnedShader = true;
                    Debug.LogWarning("[Osteon2D] " + name + ": shader '" + (tintBlack ? TintBlackShaderName : DefaultShaderName) +
                                     "' not found, drawing with Sprites/Default (blend modes and tint black will be wrong). Select the Skeleton Data asset in the editor so it stores its shader references.");
                }
            }
            m = new Material(s) { name = name + "_page" + page + "_" + blend };
            if (pageTextures != null && page < pageTextures.Length) m.mainTexture = pageTextures[page];
            ApplyBlend(m, blend);
            m.hideFlags = HideFlags.DontSave;
            runtimeMaterials[key] = m;
            return m;
        }

        /// <summary>Sets the material's blend factors for a slot blend mode (straight alpha atlas).</summary>
        public static void ApplyBlend(Material m, BlendMode blend)
        {
            // UnityEngine.Rendering.BlendMode: One = 1, OneMinusSrcAlpha = 10, SrcAlpha = 5, DstColor = 2, OneMinusSrcColor = 6
            int src, dst;
            switch (blend)
            {
                case BlendMode.Additive: src = 5; dst = 1; break;
                case BlendMode.Multiply: src = 2; dst = 10; break;
                case BlendMode.Screen: src = 1; dst = 6; break;
                default: src = 5; dst = 10; break;
            }
            m.SetFloat("_SrcBlend", src);
            m.SetFloat("_DstBlend", dst);
        }
    }
}
