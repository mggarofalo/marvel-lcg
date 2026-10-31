using System.Collections.Immutable;
using Marvel.Cards.Run;
using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.State;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Events;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DamageReplacementProjectionTests : CoreCardTestBase
{
    [Rule("rr:villain-defeat.3.1")]
    [Rule("rr:villain-defeat.3.2")]
    [Theory]
    [InlineData("player", 3, false)]
    [InlineData("minion", 3, false)]
    [InlineData("villain", 3, true)]
    [InlineData("successor", 3, true)]
    public void ArmoredRhinoSuitOnlyReplacesDamageToItsOwnCharacter(string recipient, int amount, bool replaces)
    {
        // For same-title stages, "The new stage of the villain is considered the same
        // character as the defeated stage for purposes of card abilities" and
        // "Attachments, upgrades, status cards, counters, and non-damage tokens on a
        // villain carry over to the new stage."
        var world = Hero("01001a");
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var armor = world.CreateCard("01098", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, villain.ObjectId));
        var next = world.CreateCard("01095", world.AreaOf(DeckType.VillainDeck));
        var minion = world.CreateCard("01182", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var target = recipient switch
        {
            "player" => world.Seats[0].IdentityCard,
            "minion" => minion,
            "successor" => next,
            _ => villain,
        };
        var source = world.CreateCard("01174", world.AreaOf(DeckType.EncounterDeck));
        var query = new AbilityQueryContext(world, source, new Occurrence(0, []), 0,
            source.Incarnation, null, null, null, []);
        var expressions = new AbilityExpressionContext(query, ImmutableDictionary<string, long>.Empty,
            [], "", 0, false, null);
        var scope = new AbilityAdmission.AbilityAdmissionScope(new AbilityAdmissionContext(
            AuthoredCards.Program, world.ResourceAbilities, expressions, new(), null), []);
        var damage = new Dictionary<int, long>();
        var discarded = new HashSet<int>();
        // Printed01098 only replaces damage that would be dealt to Rhino.
        long remainingDamage = AbilityDamageProtectionTrace.AfterForcedDamageReplacements(
            scope, target.ObjectId, amount, damage, discarded, recipient == "successor" ? next.ObjectId : villain.ObjectId);
        Assert.Equal(replaces ? 0 : amount, remainingDamage);
        Assert.Equal(replaces ? amount : 0, damage.GetValueOrDefault(armor.ObjectId));
        Assert.Equal(replaces, damage.ContainsKey(armor.ObjectId));
        Assert.Empty(discarded);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AuthoredPhotonicBlastUsesItsSelectedRecipientWithRhinoArmorPresent(bool minion)
    {
        Assert.Equal(PlayBlast(minion, false), PlayBlast(minion, true));
    }

    private static (string Digest, long Random, string Events) PlayBlast(bool minion, bool probe)
    {
        Card? blast = null;
        Card? armor = null;
        Card? target = null;
        Card? payment = null;
        var runner = AuthoredCards.Runner();
        var (game, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TurnTo("01010a");
            blast = board.CreateCard("01013", board.Seats[0].Hand);
            payment = board.CreateCard("01014", board.Seats[0].Hand);
            var villain = board.TheCardIn(DeckType.VillainArea)!;
            armor = board.CreateCard("01098", board.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, villain.ObjectId));
            target = minion ? board.CreateCard("01182", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))) : villain;
        }, heroes: ["captain_marvel"], abilities: runner, scenario: "rhino");
        string before = world.Digest().Canonical();
        long random = world.Random.Generator.WordsConsumed;
        var action = Assert.Single(game.Pending!.Affordances,
            option => option.AnchorId == blast!.ObjectId && option.Verb == Game.ActionVerb);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random, world.Random.Generator.WordsConsumed);
        if (probe)
        {
            for (int attempt = 0; attempt < 3; attempt++)
                Assert.Contains(runner.Actions(world, 0), offered => offered.Card == blast!.ObjectId);
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal(random, world.Random.Generator.WordsConsumed);
        }
        int[] paid = [payment!.ObjectId];
        var allocation = ResourcePayment.Allocate(Assert.Single(action.Costs!), paid);
        var events = game.Resolve(new Decision(action.Id, [], paid, Allocations: allocation)).Events.ToList();
        var drawn = world.Seats[0].Deck.Cards[^1];
        var targetOffer = Assert.Single(game.Pending!.Affordances, offered => offered.AnchorId == target!.ObjectId);
        events.AddRange(game.Resolve(Decision.Take(targetOffer.Id)).Events);
        while (game.Pending is { Asking: Question.Opportunity })
            events.AddRange(game.Resolve(Decision.Decline).Events);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(DeckType.DiscardPile, blast!.Area.Type);
        Assert.Same(world.Seats[0].Hand, drawn.Area);
        AssertSelectedRecipient(world, armor!, target!, minion, events.OfType<FieldSet>());
        return (world.Digest().Canonical(), world.Random.Generator.WordsConsumed,
            System.Text.Json.JsonSerializer.Serialize(events));
    }

    private static void AssertSelectedRecipient(World world, Card armor, Card target, bool minion,
        IEnumerable<FieldSet> changes)
    {
        Assert.Equal(0, armor.Damage);
        Assert.Equal(!minion, changes.Any(change =>
            change.Card == armor.ObjectId && change.Field == "k_damage" && change.To == 5));
        Assert.Equal(minion ? DeckType.UpgradesArea : DeckType.EncounterDiscardPile, armor.Area.Type);
        Assert.Equal(minion ? DeckType.EncounterDiscardPile : DeckType.VillainArea, target.Area.Type);
        Assert.Equal(minion ? 1 : 0, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }
}
