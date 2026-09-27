using UnityEditor;
using UnityEngine;

/// <summary>
/// First launch window plus or manual Level Tools > Setup entry.
/// </summary>
public class ProjectSetupWindow : EditorWindow
{
    private const string SeenKey = "GOILevelTools.SetupSeen";

    private Vector2 scroll;
    private bool openedAutomatically;

    [MenuItem("Level Tools/Setup")]
    public static void Open()
    {
        var window = GetWindow<ProjectSetupWindow>(true, "Level Tools Setup");
        window.minSize = new Vector2(420, 200);
        window.openedAutomatically = false;
        window.Show();
    }

    /// <summary>
    /// Opens the window directly once per project when steps are pending.
    /// Called from InitializeOnLoad so it runs after package install.
    /// </summary>
    public static void MaybeShowOnFirstLaunch()
    {
        if (EditorPrefs.GetBool(SeenKey, false))
            return;

        EditorPrefs.SetBool(SeenKey, true);

        // Only pop when something actually needs doing.
        if (ProjectSetupRegistry.Pending().Count == 0)
            return;

        var window = GetWindow<ProjectSetupWindow>(true, "Level Tools Setup");
        window.minSize = new Vector2(420, 200);
        window.openedAutomatically = true;
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Project Setup", EditorStyles.boldLabel);

        if (openedAutomatically)
        {
            EditorGUILayout.HelpBox(
                "The level tools noticed this project is missing settings it expects (e.g. layer names). Apply them below.",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField(
                "Applies the project settings custom levels expect. Only steps that still need doing are listed.",
                EditorStyles.wordWrappedLabel);
        }

        EditorGUILayout.Space();

        var pending = ProjectSetupRegistry.Pending();

        if (pending.Count == 0)
        {
            EditorGUILayout.HelpBox("Everything is set up.", MessageType.Info);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (var step in pending)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(step.Title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(step.Description, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(2);

            if (GUILayout.Button("Apply " + step.Title, GUILayout.Height(24)))
            {
                string error = RunStep(step);
                if (!string.IsNullOrEmpty(error))
                    EditorUtility.DisplayDialog("Setup Failed", error, "OK");
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Apply All", GUILayout.Height(28)))
        {
            var failures = new System.Collections.Generic.List<string>();
            foreach (var step in ProjectSetupRegistry.Pending())
            {
                string error = RunStep(step);
                if (!string.IsNullOrEmpty(error))
                    failures.Add(step.Title + ": " + error);
            }

            if (failures.Count > 0)
                EditorUtility.DisplayDialog("Setup Finished With Errors", string.Join("\n", failures), "OK");
        }
    }

    private static string RunStep(ProjectSetupStep step)
    {
        try
        {
            return step.Apply();
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            return e.Message;
        }
    }
}

/// <summary>
/// Fires once per editor launch
/// </summary>
[InitializeOnLoad]
public static class ProjectSetupFirstLaunch
{
    static ProjectSetupFirstLaunch()
    {
        EditorApplication.delayCall += ProjectSetupWindow.MaybeShowOnFirstLaunch;
    }
}
