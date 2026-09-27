using System.Collections.Generic;
using System.Linq;
using UnityEditor;

/// <summary>
/// Applies the required layers.
/// </summary>
public static class ProjectSetupLayers
{
    private const string TagManagerPath = "ProjectSettings/TagManager.asset";

    // Layer index to expected name. Matches the game's own project settings.
    public static readonly IReadOnlyDictionary<int, string> Expected = new Dictionary<int, string>
    {
        { 8, "Player" },
        { 9, "Pole" },
        { 10, "Terrain" },
        { 11, "Sky" },
        { 12, "Background" },
        { 13, "FogVolumeShadowCaster" },
        { 14, "FogVolume" },
        { 15, "FogVolumeSurrogate" },
        { 16, "Tree" },
        { 17, "FogVolumeUniform" },
        { 18, "Rope" },
        { 19, "Illuminator" },
        { 20, "Bat" },
        { 21, "PostFx" },
        { 22, "PostFxBG" },
    };

    /// <summary>
    /// True when every expected layer already has the right name.
    /// </summary>
    public static bool IsApplied()
    {
        var layers = LoadLayers();
        if (layers == null)
            return false;

        return Expected.All(entry => layers.GetArraySizeSafe(entry.Key) && layers.GetLayerName(entry.Key) == entry.Value);
    }

    /// <summary>
    /// Writes any wrong or missing layer name. Returns null on success,
    /// otherwise a short reason the window can show.
    /// </summary>
    public static string Apply()
    {
        var layers = LoadLayers();
        if (layers == null)
            return "Could not open " + TagManagerPath + ".";

        bool changed = false;
        foreach (var entry in Expected)
        {
            if (!layers.GetArraySizeSafe(entry.Key))
                return "Layer " + entry.Key + " is outside the TagManager range.";

            if (layers.GetLayerName(entry.Key) != entry.Value)
            {
                layers.SetLayerName(entry.Key, entry.Value);
                changed = true;
            }
        }

        if (changed)
            layers.Apply();

        return null;
    }

    private static TagManagerLayers LoadLayers()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(TagManagerPath);
        if (assets == null || assets.Length == 0)
            return null;

        return new TagManagerLayers(new SerializedObject(assets[0]));
    }

    // Small wrapper so the layer logic reads plainly at the call site.
    private sealed class TagManagerLayers
    {
        private readonly SerializedObject tagManager;
        private readonly SerializedProperty layers;

        public TagManagerLayers(SerializedObject tagManager)
        {
            this.tagManager = tagManager;
            layers = tagManager.FindProperty("layers");
        }

        public bool GetArraySizeSafe(int index) =>
            layers != null && index >= 0 && index < layers.arraySize;

        public string GetLayerName(int index) =>
            layers.GetArrayElementAtIndex(index).stringValue ?? string.Empty;

        public void SetLayerName(int index, string name) =>
            layers.GetArrayElementAtIndex(index).stringValue = name;

        public void Apply()
        {
            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }
    }
}
