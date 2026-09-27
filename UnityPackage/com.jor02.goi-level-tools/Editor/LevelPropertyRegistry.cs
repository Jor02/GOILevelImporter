using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Value kinds a level property can hold. Stored in the level asset as
/// strings so they round trip through the .glf property bag unchanged.
/// </summary>
public enum LevelPropertyType
{
    String,
    Int,
    Float,
    Bool,
    Flag,
    Enum,
    Color,
}

/// <summary>
/// One known level property: how to label it, edit it, and encode it.
/// Flag is the legacy "r means on" encoding used by sky and lighting.
/// Bool encodes as "1"/"0" for the int style flags like shadow.
/// Enum draws a dropdown; EnumLabels are shown, EnumValues are stored.
/// Color draws an enable toggle plus picker; empty string means off.
/// </summary>
public readonly struct LevelPropertyDef
{
    public readonly string Key;
    public readonly string Label;
    public readonly string Tooltip;
    public readonly LevelPropertyType Type;
    public readonly string DefaultValue;
    public readonly string Category;
    public readonly string[] EnumLabels;
    public readonly string[] EnumValues;

    public LevelPropertyDef(string key, string label, string tooltip, LevelPropertyType type, string defaultValue, string category = "", string[] enumLabels = null, string[] enumValues = null)
    {
        Key = key;
        Label = label;
        Tooltip = tooltip;
        Type = type;
        DefaultValue = defaultValue;
        Category = category ?? string.Empty;
        EnumLabels = enumLabels ?? new string[0];
        EnumValues = enumValues ?? new string[0];
    }
}

/// <summary>
/// Known level properties. The editor draws one field per entry and the
/// builder writes the saved values into the .glf metadata bag.
/// </summary>
public static class LevelPropertyRegistry
{
    private static readonly LevelPropertyDef[] Ordered =
    {
        new LevelPropertyDef("mode", "Mode", "Replacement strips everything except the player, cameras, and core objects. Normal keeps the level's scenes as they are.", LevelPropertyType.Enum, "r", "Level", new[] { "Replacement", "Normal" }, new[] { "r", "" }),
        new LevelPropertyDef("zplane", "Z Plane", "How far behind the player the camera sits. More negative pulls it further back.", LevelPropertyType.Float, "-20", "Camera"),
        new LevelPropertyDef("farplane", "Far Plane", "How far the main camera can see before things get clipped away.", LevelPropertyType.Float, "100", "Camera"),
        new LevelPropertyDef("bgfarplane", "Background Far Plane", "How far the background camera can see. Needs to be huge for the sky.", LevelPropertyType.Float, "2800", "Camera"),
        new LevelPropertyDef("cam", "Camera Mode", "2D uses the game's flat orthographic camera. 3D switches to perspective and also applies the hammer material fix.", LevelPropertyType.Enum, "0", "Camera", new[] { "2D", "3D" }, new[] { "0", "1" }),
        new LevelPropertyDef("fog", "Fog Tint", "Tint the level fog. Unticked leaves fog off.", LevelPropertyType.Color, "", "World"),
        new LevelPropertyDef("sky", "Replace Sky", "Keeps the level's own sky instead of the game's clouds and skysphere.", LevelPropertyType.Flag, "", "World"),
        new LevelPropertyDef("lighting", "Replace Lighting", "Flattens the ambient lighting so the level's own lights carry the scene.", LevelPropertyType.Flag, "", "World"),
        new LevelPropertyDef("shadow", "Hide Shadow", "Hides the fake blob shadow attached to the hammer tip.", LevelPropertyType.Bool, "0", "World"),
        new LevelPropertyDef("hammermat", "Fix Hammer Material", "Swaps the hammer mesh to the Standard shader. Forced on in 3D camera mode.", LevelPropertyType.Bool, "0", "World"),
    };

    /// <summary>
    /// Name to value type map for every known property.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, LevelPropertyType> Types = BuildTypeMap();

    /// <summary>
    /// Ordered defs for drawing the inspector foldout.
    /// </summary>
    public static readonly ReadOnlyCollection<LevelPropertyDef> Properties =
        new ReadOnlyCollection<LevelPropertyDef>(Ordered);

    public static bool TryGet(string key, out LevelPropertyDef def)
    {
        foreach (var entry in Ordered)
        {
            if (entry.Key == key)
            {
                def = entry;
                return true;
            }
        }

        def = default;
        return false;
    }

    private static IReadOnlyDictionary<string, LevelPropertyType> BuildTypeMap()
    {
        var map = new Dictionary<string, LevelPropertyType>(Ordered.Length);
        foreach (var entry in Ordered)
        {
            map[entry.Key] = entry.Type;
        }

        return map;
    }
}
