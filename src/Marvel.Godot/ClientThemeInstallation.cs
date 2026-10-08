using Godot;

namespace Marvel.Godot;

/// <summary>Installs fresh themes while bounding their temporary managed ownership.</summary>
internal static class ClientThemeInstallation
{
    internal static void Apply(Control control, InterfaceScale scale = InterfaceScale.Standard)
    {
        // The native owner retains the theme after this temporary wrapper is released.
        using var theme = ClientTheme.Create(scale);
        control.Theme = theme;
    }

    internal static void Apply(Window window, InterfaceScale scale = InterfaceScale.Standard)
    {
        // The native owner retains the theme after this temporary wrapper is released.
        using var theme = ClientTheme.Create(scale);
        window.Theme = theme;
    }
}
