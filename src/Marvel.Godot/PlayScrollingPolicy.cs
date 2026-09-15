using Godot;

namespace Marvel.Godot;

/// <summary>Chooses page scrolling from the visible play surface.</summary>
internal static class PlayScrollingPolicy
{
    internal static ScrollContainer.ScrollMode PageVerticalScrollMode(
        bool boardVisible,
        bool invitationVisible,
        InterfaceScale scale) => !boardVisible || invitationVisible || (int)scale > 100
            ? ScrollContainer.ScrollMode.Auto
            : ScrollContainer.ScrollMode.Disabled;
}
