using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Osteon2D.Editor
{
    /// <summary>Imports Osteon2D project files (.osteon, which are Spine-format JSON plus an editor block) as text assets.</summary>
    [ScriptedImporter(1, "osteon")]
    public class OsteonProjectImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var text = new TextAsset(File.ReadAllText(ctx.assetPath));
            ctx.AddObjectToAsset("skeleton", text);
            ctx.SetMainObject(text);
        }
    }

    /// <summary>Imports libGDX / Spine style .atlas files as text assets.</summary>
    [ScriptedImporter(1, "atlas")]
    public class OsteonAtlasImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var text = new TextAsset(File.ReadAllText(ctx.assetPath));
            ctx.AddObjectToAsset("atlas", text);
            ctx.SetMainObject(text);
        }
    }

    /// <summary>Right click a skeleton file in the Project window: Create > Osteon2D > Skeleton Data From Selection.</summary>
    public static class OsteonCreateAssets
    {
        static bool IsSkeletonPath(string p)
        {
            string l = p.ToLowerInvariant();
            return l.EndsWith(".osteon") || l.EndsWith(".json");
        }

        [MenuItem("Assets/Create/Osteon2D/Skeleton Data From Selection", true)]
        static bool Validate()
        {
            var o = Selection.activeObject as TextAsset;
            if (o == null) return false;
            return IsSkeletonPath(AssetDatabase.GetAssetPath(o));
        }

        /// <summary>The atlas next to the skeleton: <name>.atlas first, otherwise the only .atlas in that same folder (not its subfolders).</summary>
        static TextAsset FindAtlas(string dir, string baseName, out string problem)
        {
            problem = null;
            var exact = AssetDatabase.LoadAssetAtPath<TextAsset>(dir + "/" + baseName + ".atlas");
            if (exact != null) return exact;
            var found = new List<string>();
            foreach (var g in AssetDatabase.FindAssets("t:TextAsset", new[] { dir }))
            {
                string ap = AssetDatabase.GUIDToAssetPath(g);
                if (ap.ToLowerInvariant().EndsWith(".atlas") && Path.GetDirectoryName(ap).Replace('\\', '/') == dir) found.Add(ap);
            }
            if (found.Count == 1) return AssetDatabase.LoadAssetAtPath<TextAsset>(found[0]);
            if (found.Count > 1) problem = "Several .atlas files are in " + dir + " and none is named " + baseName + ".atlas. Rename the right one to " + baseName + ".atlas.";
            else problem = "No .atlas file was found next to the skeleton in " + dir + ". In Osteon2D use File > Texture Packer, name the atlas " + baseName + ", and put the .atlas and its PNG pages in the same folder.";
            return null;
        }

        /// <summary>
        /// Atlas pages are not sprites or regular textures: no mipmaps (they bleed between packed images), no compression
        /// (artifacts on edges), Clamp wrapping, and alpha-is-transparency so edges do not get dark fringes. Returns true when
        /// something had to be changed.
        /// </summary>
        public static bool ConfigurePageTexture(string texturePath)
        {
            var ti = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (ti == null) return false;
            bool changed = ti.textureType != TextureImporterType.Default || !ti.alphaIsTransparency || ti.mipmapEnabled ||
                           ti.wrapMode != TextureWrapMode.Clamp || ti.npotScale != TextureImporterNPOTScale.None ||
                           ti.textureCompression != TextureImporterCompression.Uncompressed || !ti.sRGBTexture ||
                           ti.alphaSource != TextureImporterAlphaSource.FromInput;
            if (!changed) return false;
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
            return true;
        }

        [MenuItem("Assets/Create/Osteon2D/Skeleton Data From Selection")]
        static void Create()
        {
            var skeleton = (TextAsset)Selection.activeObject;
            string path = AssetDatabase.GetAssetPath(skeleton);
            string dir = Path.GetDirectoryName(path).Replace('\\', '/');
            string baseName = Path.GetFileNameWithoutExtension(path);

            string problem;
            TextAsset atlas = FindAtlas(dir, baseName, out problem);
            if (atlas == null) { EditorUtility.DisplayDialog("Osteon2D", problem, "OK"); return; }

            var shader = Shader.Find(OsteonSkeletonDataAsset.DefaultShaderName);
            var tintShader = Shader.Find(OsteonSkeletonDataAsset.TintBlackShaderName);
            if (shader == null)
            {
                EditorUtility.DisplayDialog("Osteon2D", "The shader '" + OsteonSkeletonDataAsset.DefaultShaderName + "' was not found. Check the Console for shader compile errors in the Osteon2D package, then try again.", "OK");
                return;
            }

            var parsed = Atlas.Parse(atlas.text);
            string atlasDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(atlas)).Replace('\\', '/');
            var textures = new Texture2D[parsed.pages.Count];
            var missing = new List<string>();
            int fixedTextures = 0;
            for (int i = 0; i < textures.Length; i++)
            {
                string tp = atlasDir + "/" + parsed.pages[i].file;
                if (ConfigurePageTexture(tp)) fixedTextures++;
                textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(tp);
                if (textures[i] == null) missing.Add(parsed.pages[i].file);
            }

            var asset = ScriptableObject.CreateInstance<OsteonSkeletonDataAsset>();
            asset.skeletonJson = skeleton;
            asset.atlasText = atlas;
            asset.pageTextures = textures;
            asset.shader = shader;
            asset.tintBlackShader = tintShader;
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(dir + "/" + baseName + "_SkeletonData.asset");
            AssetDatabase.CreateAsset(asset, assetPath);

            // One material per page and blend mode, stored inside the data asset so they can be tweaked or shared.
            var materials = new Material[textures.Length * 4];
            for (int p = 0; p < textures.Length; p++)
                for (int b = 0; b < 4; b++)
                {
                    var m = new Material(shader) { name = baseName + "_page" + p + "_" + (BlendMode)b };
                    m.mainTexture = textures[p];
                    OsteonSkeletonDataAsset.ApplyBlend(m, (BlendMode)b);
                    AssetDatabase.AddObjectToAsset(m, asset);
                    materials[p * 4 + b] = m;
                }
            asset.materials = materials;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;

            if (fixedTextures > 0) Debug.Log("[Osteon2D] Set " + fixedTextures + " atlas page texture(s) to atlas import settings (no mipmaps, no compression, Clamp, alpha is transparency).");
            if (tintShader == null) Debug.LogWarning("[Osteon2D] The tint black shader '" + OsteonSkeletonDataAsset.TintBlackShaderName + "' was not found; tint black will not work.");
            if (missing.Count > 0)
                EditorUtility.DisplayDialog("Osteon2D", "Created " + Path.GetFileName(assetPath) + ", but these atlas page images were not found next to the atlas:\n\n" +
                                            string.Join("\n", missing.ToArray()) + "\n\nCopy them into " + atlasDir + " and run Skeleton Data From Selection again.", "OK");
        }

        [MenuItem("GameObject/Osteon2D/Skeleton Animation", false, 10)]
        static void CreateObject()
        {
            var go = new GameObject("Osteon2D Skeleton");
            go.AddComponent<OsteonSkeletonAnimation>();
            if (Selection.activeObject is OsteonSkeletonDataAsset) go.GetComponent<OsteonSkeletonAnimation>().skeletonDataAsset = (OsteonSkeletonDataAsset)Selection.activeObject;
            Undo.RegisterCreatedObjectUndo(go, "Create Osteon2D Skeleton");
            Selection.activeGameObject = go;
        }
    }
}
