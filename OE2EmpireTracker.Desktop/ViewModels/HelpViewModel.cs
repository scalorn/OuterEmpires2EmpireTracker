using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using OE2EmpireTracker.Desktop.Services;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// ViewModel for the Help document tab.
/// Displays a topic list on the left and rendered markdown content on the right.
/// </summary>
public sealed partial class HelpViewModel : DocumentViewModel
{
    [ObservableProperty]
    private HelpTopic? _selectedTopic;

    [ObservableProperty]
    private string _content = string.Empty;

    public HelpViewModel()
    {
        Title = "Help";
        Topics = new ObservableCollection<HelpTopic>();
        LoadTopics();
    }

    public ObservableCollection<HelpTopic> Topics { get; }

    partial void OnSelectedTopicChanged(HelpTopic? value)
    {
        if (value is null)
        {
            Content = string.Empty;
            return;
        }

        var renderer = App.Services?.GetService<HelpRenderer>();
        if (renderer is not null)
        {
            Content = renderer.RenderTopic(value.FileName);
        }
    }

    private void LoadTopics()
    {
        var registry = App.Services?.GetService<HelpTopicRegistry>();
        if (registry is null)
        {
            Content = "# OE2 Empire Tracker Help\n\nHelp system is loading...";
            return;
        }

        var allTopics = registry.GetAllTopics();
        foreach (var (displayName, fileName) in allTopics)
        {
            Topics.Add(new HelpTopic(displayName, fileName));
        }

        // Select the first topic by default
        if (Topics.Count > 0)
        {
            SelectedTopic = Topics[0];
        }
    }
}

/// <summary>
/// Represents a single help topic entry in the topic list.
/// </summary>
public sealed class HelpTopic
{
    public HelpTopic(string displayName, string fileName)
    {
        DisplayName = displayName;
        FileName = fileName;
    }

    public string DisplayName { get; }

    public string FileName { get; }

    public override string ToString() => DisplayName;
}
