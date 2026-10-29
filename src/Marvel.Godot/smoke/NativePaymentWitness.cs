using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Read-only payment evidence from the current authorized client response.</summary>
public sealed partial class NativePaymentWitness : RefCounted
{
    private Main? table;

    /// <summary>Binds the table being observed.</summary>
    public void Bind(Main main) => table = main;

    /// <summary>Returns physical visible cards of a face in one seat's specified area.</summary>
    public int[] Cards(string face, string zone, int owner) => table?.CurrentGame?.World?.Areas
        .Where(area => area.Zone == zone && area.Owner == owner)
        .SelectMany(area => area.Cards)
        .Where(card => card.Face?.Id == face && card.Id is not null)
        .Select(card => card.Id!.Value).ToArray() ?? [];

    /// <summary>Reports whether the exact visible physical card occupies an area.</summary>
    public bool InArea(int id, string zone, int owner) => table?.CurrentGame?.World?.Areas
        .Any(area => area.Zone == zone && area.Owner == owner && area.Cards.Any(card => card.Id == id)) == true;

    /// <summary>Reports readiness for a visible physical card.</summary>
    public bool Ready(int id) => Card(id)?.Ready == true;

    /// <summary>Reports selected physical resource sources in the current draft.</summary>
    public int[] DraftResources() => table?.decisions.composer?.Resources.ToArray() ?? [];

    /// <summary>Reports the single engine-offered price for a physical card, or an empty string.</summary>
    public string OfferedPrice(int id) => table?.CurrentGame?.Prompt?.Affordances
        .SingleOrDefault(offer => offer.AnchorId == id)?.CostOptions.SingleOrDefault()?.Cost ?? string.Empty;

    /// <summary>Reports visible damage, or -1 when the card is unavailable.</summary>
    public long Damage(int id) => Card(id)?.Face?.Damage ?? -1;

    /// <summary>Reports a visible counter, or -1 when the card is unavailable.</summary>
    public long Counter(int id, string name) => Card(id)?.Face is { } face
        ? face.Counters.GetValueOrDefault(name) : -1;

    /// <summary>Reports a visible effective value, or -1 when unavailable.</summary>
    public long Value(int id, string name) => Card(id)?.Face?.EffectiveValues.TryGetValue(name, out var value) == true
        ? value.CurrentValue : -1;

    private CardDescriptor? Card(int id) => table?.CurrentGame?.World?.Areas.SelectMany(area => area.Cards)
        .SingleOrDefault(card => card.Id == id && card.Face is not null);
}
