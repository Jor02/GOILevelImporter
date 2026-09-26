using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomLevelObject : ScriptableObject
{
    public string LevelName;
    public string Author;
    [TextArea(3, 5)]
    public string Description;

    [Header("Thumbnail")]
    public Texture2D Thumbnail;

    public List<SceneAsset> LevelScenes = new List<SceneAsset>();

    private void OnValidate()
    {
        if (LevelScenes == null || LevelScenes.Count < 2)
        {
            return;
        }

        var seen = new HashSet<SceneAsset>();
        for (int i = LevelScenes.Count - 1; i >= 0; i--)
        {
            if (!seen.Add(LevelScenes[i]))
            {
                LevelScenes.RemoveAt(i);
            }
        }
    }

    [MenuItem("Assets/Create/Custom Level", false, 1)]
    private static void CreateFromScene(MenuCommand command)
    {
        if (command.context is not SceneAsset scene)
        {
            return;
        }

        string directory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(scene)) ?? "Assets";

        string assetPath = AssetDatabase.GenerateUniqueAssetPath(
            $"{directory}/{scene.name} Level.asset");

        var level = CreateInstance<CustomLevelObject>();
        level.LevelName = scene.name;
        level.LevelScenes = new List<SceneAsset> { scene };

        AssetDatabase.CreateAsset(level, assetPath);

        Undo.RegisterCreatedObjectUndo(level, "Create Custom Level");
        EditorGUIUtility.PingObject(level);
    }

    [MenuItem("Assets/Create/Custom Level", true, 1)]
    private static bool ValidateCreateFromScene(MenuCommand command)
    {
        return command.context is SceneAsset;
    }
}