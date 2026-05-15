using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;

namespace OE2EmpireTracker.Desktop.Controls;

/// <summary>
/// TextBox with built-in regex validation and error display.
/// Equivalent to the WinForms ValidatedTextBox control.
/// </summary>
public class ValidatedTextBox : TextBox
{
    /// <summary>
    /// Defines the <see cref="ValidationPattern"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> ValidationPatternProperty =
        AvaloniaProperty.Register<ValidatedTextBox, string?>(nameof(ValidationPattern));

    /// <summary>
    /// Defines the <see cref="ErrorMessage"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> ErrorMessageProperty =
        AvaloniaProperty.Register<ValidatedTextBox, string?>(nameof(ErrorMessage));

    /// <summary>
    /// Defines the <see cref="IsValid"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsValidProperty =
        AvaloniaProperty.Register<ValidatedTextBox, bool>(nameof(IsValid), defaultValue: true);

    private static readonly IBrush ErrorBorderBrush = new SolidColorBrush(Colors.Red);
    private IBrush? _originalBorderBrush;

    public ValidatedTextBox()
    {
        PropertyChanged += OnPropertyChangedHandler;
    }

    /// <summary>Gets or sets the regex pattern for validation. Null means no validation.</summary>
    public string? ValidationPattern
    {
        get => GetValue(ValidationPatternProperty);
        set => SetValue(ValidationPatternProperty, value);
    }

    /// <summary>Gets or sets the error message shown when validation fails.</summary>
    public string? ErrorMessage
    {
        get => GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    /// <summary>Gets whether the current text passes validation.</summary>
    public bool IsValid
    {
        get => GetValue(IsValidProperty);
        private set => SetValue(IsValidProperty, value);
    }

    /// <summary>
    /// Sets an external validation error (e.g., duplicate name check).
    /// </summary>
    public void SetError(string message)
    {
        ErrorMessage = message;
        IsValid = false;
        ApplyErrorVisual();
    }

    /// <summary>
    /// Clears any external validation error.
    /// </summary>
    public void ClearError()
    {
        ErrorMessage = null;
        Validate();
    }

    private void OnPropertyChangedHandler(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextProperty || e.Property == ValidationPatternProperty)
        {
            Validate();
        }
    }

    private void Validate()
    {
        var pattern = ValidationPattern;
        if (string.IsNullOrEmpty(pattern))
        {
            IsValid = true;
            ClearErrorVisual();
            return;
        }

        var text = Text ?? string.Empty;
        if (string.IsNullOrEmpty(text))
        {
            IsValid = true;
            ClearErrorVisual();
            return;
        }

        try
        {
            IsValid = Regex.IsMatch(text, pattern);
        }
        catch
        {
            IsValid = false;
        }

        if (IsValid)
        {
            ClearErrorVisual();
        }
        else
        {
            ApplyErrorVisual();
        }
    }

    private void ApplyErrorVisual()
    {
        _originalBorderBrush ??= BorderBrush;
        BorderBrush = ErrorBorderBrush;
    }

    private void ClearErrorVisual()
    {
        if (_originalBorderBrush is not null)
        {
            BorderBrush = _originalBorderBrush;
            _originalBorderBrush = null;
        }
    }
}
