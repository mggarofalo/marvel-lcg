using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Read-only game-area and Kang facts from the response the table displays, for native assertions.</summary>
public sealed partial class KangNativeWitness : RefCounted
{
    private Main? table;

    private WorldDescriptor? World => table?.CurrentGame?.World;

    /// <summary>Binds the visible table being observed.</summary>
    public void Bind(Main main) => table = main;

    /// <summary>The Kang stage, or zero when the response has no Kang facts.</summary>
    public int Stage() => World?.Kang?.Stage ?? 0;

    /// <summary>The ids of the game areas that hold no villain play area, in engine order.</summary>
    public int[] LocalAreas() => [.. World?.GameAreas.Where(area => !area.Central).Select(area => area.Id) ?? []];

    /// <summary>The engine-authored label of one game area.</summary>
    public string Label(int area) => World?.GameAreas.FirstOrDefault(item => item.Id == area)?.Label ?? string.Empty;

    /// <summary>The villains in one game area.</summary>
    public int[] Villains(int area) => [.. World?.GameAreas.FirstOrDefault(item => item.Id == area)?.Villains ?? []];

    /// <summary>One seat's game area, or -1 when it is in none.</summary>
    public int SeatArea(int seat) => World?.Players.FirstOrDefault(item => item.Seat == seat)?.GameArea ?? -1;

    /// <summary>The cards one seat's cards cannot affect.</summary>
    public int[] OutOfReach(int seat) => [.. World?.Players.FirstOrDefault(item => item.Seat == seat)?.OutOfReach ?? []];

    /// <summary>The main schemes in no game area.</summary>
    public int[] CentralSchemes() => [.. World?.Kang?.CentralSchemes ?? []];

    /// <summary>The game area acting in the villain phase, or -1.</summary>
    public int ActingArea() => World?.Kang?.Acting?.GameArea ?? -1;

    /// <summary>The game areas a Kang (II) group leaves at the end of the phase.</summary>
    public int[] DepartureSources() => [.. World?.Kang?.Departures.Select(item => item.SourceGameArea) ?? []];

    /// <summary>The game areas retained for eliminated players.</summary>
    public int[] RetainedAreas() => [.. World?.GameAreas.Where(area => area.Retained).Select(area => area.Id) ?? []];

    /// <summary>The engine trigger of the displayed prompt.</summary>
    public string Trigger() => table?.CurrentGame?.Prompt?.Trigger ?? string.Empty;
}
