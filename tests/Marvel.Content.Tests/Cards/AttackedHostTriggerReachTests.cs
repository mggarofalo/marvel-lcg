using System.Text.Json;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Cards;

/// <summary>
/// Characterises the authored book against the one attack-recipient shape that
/// still raises: simultaneous attack damage reaching, after the attack's
/// initiation, an enemy whose attachment answers its host being attacked.
/// </summary>
/// <remarks>
/// A later recipient of separately placed attack damage is announced by its own
/// attack-initiated occurrence. Simultaneous placement is not, so
/// <c>AbilityAttackRecipients.RequireNoSimultaneousLateAnswer</c> raises there.
/// These facts keep every authored card, admitted or not, away from it: the only
/// simultaneous recipients reached after initiation are minions, and every
/// "when [host] is attacked" attachment a player attack can reach attaches to a
/// villain.
/// </remarks>
public sealed class AttackedHostTriggerReachTests
{
    private static readonly string?[] SimultaneousSelectors = ["powerTargets", "minions"];

    [Fact]
    [Rule("rr:attack-player-ability-type.5")]
    public void EveryAttachmentAnsweringItsHostBeingAttackedAttachesToAVillain()
    {
        var villains = VillainTitles();
        var rows = Rows().Where(row => row.TryGetProperty("abilities", out var abilities)
            && abilities.EnumerateArray().Any(AnswersHostAttack)).ToArray();

        // The Accused (90003) attaches to a hero and answers only an enemy's attack
        // (`actor: "enemy"`); a player attack's recipients are enemies, so it never
        // answers one. Every other such attachment attaches to a villain.
        Assert.Equal(["04104", "11014", "90003"], rows.Select(row => row.GetProperty("card").GetString()).Order());
        foreach (var row in rows.Where(row => !row.GetProperty("abilities").EnumerateArray()
            .Where(AnswersHostAttack).All(ability => ability.GetProperty("trigger").TryGetProperty("actor", out var actor)
                && actor.GetString() == "enemy")))
        {
            var attachTo = row.GetProperty("attachTo");
            bool villain = attachTo.TryGetProperty("titled", out var title) && villains.Contains(title.GetString()!)
                || attachTo.TryGetProperty("query", out var query) && query.GetString() is "villain" or "villainInThisGameArea";
            Assert.True(villain, $"{row.GetProperty("card").GetString()} attaches to {attachTo}");
        }
    }

    [Fact]
    [Rule("rr:attack-player-ability-type.5")]
    public void SimultaneousAttackDamageReachesOnlyDeclaredRecipientsOrMinions()
    {
        var selectors = Rows().SelectMany(row => Nodes(row))
            .Where(node => node.ValueKind == JsonValueKind.Object && node.TryGetProperty("dealAttackDamage", out var damage)
                && damage.TryGetProperty("simultaneous", out _))
            .Select(node => node.GetProperty("dealAttackDamage").GetProperty("cards").GetProperty("query").GetString())
            .Distinct().Order().ToArray();

        Assert.All(selectors, selector => Assert.Contains(selector, SimultaneousSelectors));
    }

    private static bool AnswersHostAttack(JsonElement ability) =>
        ability.GetProperty("trigger") is var trigger
        && trigger.TryGetProperty("event", out var name) && name.GetString() == "WhenAttackInitiated"
        && trigger.TryGetProperty("target", out var target) && target.GetString() == "attachedTo";

    private static IEnumerable<JsonElement> Nodes(JsonElement element)
    {
        yield return element;
        IEnumerable<JsonElement> children = element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().Select(property => property.Value),
            JsonValueKind.Array => element.EnumerateArray(),
            _ => [],
        };
        foreach (var child in children)
            foreach (var node in Nodes(child)) yield return node;
    }

    private static JsonElement[] Rows()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")));
        return [.. json.RootElement.GetProperty("cards").EnumerateArray().Select(row => row.Clone())];
    }

    private static HashSet<string> VillainTitles()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        return [.. json.RootElement.GetProperty("cards").EnumerateArray()
            .Where(card => card.GetProperty("type").GetString() == "Villain")
            .Select(card => card.GetProperty("name").GetString()!)];
    }
}
