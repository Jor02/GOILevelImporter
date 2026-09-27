using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public readonly struct ProjectSetupStep
{
    public readonly string Id;
    public readonly string Title;
    public readonly string Description;
    public readonly Func<bool> IsApplied;
    public readonly Func<string> Apply;

    public ProjectSetupStep(string id, string title, string description, Func<bool> isApplied, Func<string> apply)
    {
        Id = id;
        Title = title;
        Description = description;
        IsApplied = isApplied;
        Apply = apply;
    }
}

/// <summary>
/// Known project setup steps.
/// </summary>
public static class ProjectSetupRegistry
{
    private static readonly ProjectSetupStep[] Ordered =
    {
        new ProjectSetupStep(
            "layers",
            "Layers",
            "Names the layers the game expects: Player, Pole, Terrain, Sky, Background, FogVolume layers, Tree, Rope, Illuminator, Bat, PostFxBG and PostFx.",
            ProjectSetupLayers.IsApplied,
            ProjectSetupLayers.Apply),
    };

    /// <summary>
    /// Ordered steps for the setup window.
    /// </summary>
    public static readonly ReadOnlyCollection<ProjectSetupStep> Steps =
        new ReadOnlyCollection<ProjectSetupStep>(Ordered);

    /// <summary>
    /// Steps that still need applying.
    /// </summary>
    public static List<ProjectSetupStep> Pending()
    {
        var pending = new List<ProjectSetupStep>();
        foreach (var step in Ordered)
        {
            try
            {
                if (!step.IsApplied())
                    pending.Add(step);
            }
            catch (Exception)
            {
                pending.Add(step);
            }
        }

        return pending;
    }
}
