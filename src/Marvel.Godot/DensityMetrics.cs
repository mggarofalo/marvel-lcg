namespace Marvel.Godot;

/// <summary>
/// Content insets and allowances for one supported interface scale.
///
/// Every framed boundary owns exactly one of these measurements. Child
/// containers use separation rather than repeating the parent frame's inset.
/// </summary>
public sealed record DensityMetrics(
    int ViewportInset,
    int ShellHorizontal,
    int ShellVertical,
    int SurfaceHorizontal,
    int SurfaceVertical,
    int StatusHorizontal,
    int StatusVertical,
    int BoardHorizontal,
    int BoardVertical,
    int CompactCardHorizontal,
    int CompactCardVertical,
    int FullCardHorizontal,
    int FullCardVertical,
    int InputHorizontal,
    int InputVertical,
    int ButtonHorizontal,
    int ButtonVertical,
    int PrimaryButtonHorizontal,
    int PrimaryButtonVertical,
    int ArtWellInset,
    int BoardAreaAllowance);
