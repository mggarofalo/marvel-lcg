using Godot;

namespace Marvel.Godot;

/// <summary>Installs shared scale themes without repeating ancestor theme notifications.</summary>
internal static class ClientThemeInstallation
{
    internal static void Apply(Control control, InterfaceScale scale = InterfaceScale.Standard)
    {
        Theme theme = ClientThemePalette.For(scale);
        if (control.Theme != theme) control.Theme = theme;
        SharedControlFeedback.Install(control);
        control.OversamplingWithScale = CanvasItem.OversamplingWithScaleEnum.Enabled;
    }

    internal static void Apply(Window window, InterfaceScale scale = InterfaceScale.Standard)
    {
        // A popup owns its independent theme until its native window is freed.
        using var theme = ClientTheme.Create(scale);
        window.Theme = theme;
    }
}
