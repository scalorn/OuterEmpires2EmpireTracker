using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using OE2EmpireTracker.Desktop.Services;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// ViewModel for the Help document tab.
/// Displays a topic list on the left and rendered markdown content on the right.
/// Parses internal links to show related topics as clickable buttons.
/// </summary>
public sealed partial class HelpViewModel : DocumentViewModel
{
    private static readonly Regex LinkPattern = new Regex(
        @"\[([^\]]+)\]\(([^)]+\.md)\)",
        RegexOptions.Compiled);

    [ObservableProperty]
    private HelpTopic? _selectedTopic;

    [ObservableProperty]
    private string _content = string.Empty;

    public HelpViewModel()
    {
        Title = "Help";
        Topics = new ObservableCollection<HelpTopic>();
        RelatedTopics = new ObservableCollection<RelatedTopicItem>();
        LoadTopics();
    }

    public ObservableCollection<HelpTopic> Topics { get; }

    /// <summary>
    /// Related topics extracted from markdown links in the current content.
    /// </summary>
    public ObservableCollection<RelatedTopicItem> RelatedTopics { get; }

    /// <summary>
    /// Navigates to the topic with the given file name, selecting it in the list.
    /// </summary>
    /// <param name="fileName">The help topic file name (e.g. "colonies.md").</param>
    public void NavigateToTopic(string fileName)
    {
        foreach (var topic in Topics)
        {
            if (string.Equals(topic.FileName, fileName, System.StringComparison.OrdinalIgnoreCase))
            {
                SelectedTopic = topic;
                return;
            }
        }
    }

    /// <summary>
    /// Command to navigate to an internal link target.
    /// Accepts a filename (e.g. "colonies.md") and loads that topic.
    /// </summary>
    [RelayCommand]
    private void NavigateLink(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return;
        }

        NavigateToTopic(fileName);
    }

    partial void OnSelectedTopicChanged(HelpTopic? value)
    {
        if (value is null)
        {
            Content = string.Empty;
            RelatedTopics.Clear();
            return;
        }

        var renderer = App.Services?.GetService<HelpRenderer>();
        if (renderer is not null)
        {
            Content = renderer.RenderTopic(value.FileName);
        }

        ExtractRelatedTopics(Content, value.FileName);
    }

    private void ExtractRelatedTopics(string markdown, string currentFileName)
    {
        RelatedTopics.Clear();

        if (string.IsNullOrEmpty(markdown))
        {
            return;
        }

        var seen = new System.Collections.Generic.HashSet<string>(
            System.StringComparer.OrdinalIgnoreCase);

        foreach (Match match in LinkPattern.Matches(markdown))
        {
            var displayName = match.Groups[1].Value;
            var fileName = match.Groups[2].Value;

            // Skip self-references and duplicates
            if (string.Equals(fileName, currentFileName, System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!seen.Add(fileName))
            {
                continue;
            }

            RelatedTopics.Add(new RelatedTopicItem(displayName, fileName));
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

/// <summary>
/// Represents a related topic link extracted from markdown content.
/// </summary>
public sealed class RelatedTopicItem
{
    public RelatedTopicItem(string displayName, string fileName)
    {
        DisplayName = displayName;
        FileName = fileName;
    }

    public string DisplayName { get; }

    public string FileName { get; }
}
