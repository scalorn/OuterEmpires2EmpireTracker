using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;

namespace OE2EmpireTracker.Desktop.Controls;

/// <summary>
/// ComboBox with text filtering. As the user types, the dropdown items are filtered
/// to show only matching entries. Equivalent to the WinForms FilteredTextComboSet.
/// Built on top of Avalonia's AutoCompleteBox which provides this natively.
/// </summary>
public class FilteredComboBox : AutoCompleteBox
{
    /// <summary>
    /// Defines the <see cref="DisplayMemberPath"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> DisplayMemberPathProperty =
        AvaloniaProperty.Register<FilteredComboBox, string?>(nameof(DisplayMemberPath));

    public FilteredComboBox()
    {
        FilterMode = AutoCompleteFilterMode.ContainsOrdinal;
        MinimumPrefixLength = 0;
        IsTextCompletionEnabled = false;
    }

    /// <summary>Gets or sets the property path used for display text.</summary>
    public string? DisplayMemberPath
    {
        get => GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DisplayMemberPathProperty && DisplayMemberPath is not null)
        {
            TextSelector = (search, item) => GetDisplayText(item);
            ItemFilter = (search, item) => FilterItem(search, item);
        }
    }

    private string GetDisplayText(object? item)
    {
        if (item is null)
        {
            return string.Empty;
        }

        if (string.IsNullOrEmpty(DisplayMemberPath))
        {
            return item.ToString() ?? string.Empty;
        }

        var prop = item.GetType().GetProperty(DisplayMemberPath);
        return prop?.GetValue(item)?.ToString() ?? item.ToString() ?? string.Empty;
    }

    private bool FilterItem(string? search, object? item)
    {
        if (string.IsNullOrEmpty(search))
        {
            return true;
        }

        var displayText = GetDisplayText(item);
        return displayText.Contains(search, StringComparison.OrdinalIgnoreCase);
    }
}
