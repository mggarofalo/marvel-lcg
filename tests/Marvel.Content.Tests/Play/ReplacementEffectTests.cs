using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Damage that happens to something else — <c>rr:replacement-effect</c>.
/// </summary>
/// <remarks>
/// <para>
/// "When any amount of damage <b>would be</b> dealt to Rhino, place it here
/// instead." <c>rr:damage</c> lists nine steps and this is step 1, before the
/// tough card at step 2 — so a replacement leaves nothing for the rest of them
/// to do, which is <c>rr:replacement-effect.1</c>: "when an effect is replaced,
/// it is no longer considered imminent".
/// </para>
/// <para>
/// <b>Placed, not dealt.</b> The damage goes onto the attachment as tokens
/// rather than through <c>Damage.Deal</c>, which would start the nine steps
/// again on a card that is not a character.
/// </para>
/// </remarks>
public sealed class ReplacementEffectTests
{
    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:damage.step.1")]
    [Fact]
    public void TheSuitTakesTheDamageAndTheVillainTakesNone()
    {
        var (world, suit, villain) = Board();

        Damage.Deal(world, Cards, villain, villain, 3, "test", "test", []);

        Assert.Equal(3, suit.Damage);
        Assert.Equal(0, villain.Damage);
    }

    [Rule("rr:replacement-effect.1")]
    [Fact]
    public void AToughCardIsNotSpentOnDamageThatNeverArrives()
    {
        // The tough card is step 2 and the replacement is step 1, so there is
        // nothing left for it to prevent -- and a tough card spent on damage
        // that was replaced would be a tough card gone for nothing.
        var (world, suit, villain) = Board();
        Statuses.Give(world, villain, Statuses.Tough);

        Damage.Deal(world, Cards, villain, villain, 2, "test", "test", []);

        Assert.True(Statuses.Has(world, villain, Statuses.Tough));
        Assert.Equal(2, suit.Damage);
    }

    [Fact]
    public void FiveDamageOnItDiscardsIt()
    {
        // "Then, if there is at least 5 damage here, discard Armored Rhino
        // Suit." **Then** -- after the damage is placed, so the fifth point
        // lands on the suit and the suit goes.
        var (world, suit, villain) = Board();

        Damage.Deal(world, Cards, villain, villain, 4, "test", "test", []);
        Assert.Equal(DeckType.UpgradesArea, suit.Area.Type);

        Damage.Deal(world, Cards, villain, villain, 1, "test", "test", []);

        Assert.Equal(DeckType.EncounterDiscardPile, suit.Area.Type);
        Assert.Equal(0, villain.Damage);
    }

    [Fact]
    public void OnceItIsGoneTheVillainTakesTheDamageAgain()
    {
        // The whole point of the card, and the reason the discard matters: it
        // is a shield with a limit rather than a permanent one.
        var (world, suit, villain) = Board();
        Damage.Deal(world, Cards, villain, villain, 5, "test", "test", []);
        Assert.Equal(DeckType.EncounterDiscardPile, suit.Area.Type);

        Damage.Deal(world, Cards, villain, villain, 3, "test", "test", []);

        Assert.Equal(3, villain.Damage);
    }

    [Fact]
    public void ItOnlySoaksDamageAimedAtWhatItIsAttachedTo()
    {
        // "Damage would be dealt **to Rhino**". The subject is the attached
        // card, not the suit, so a minion taking damage across the table is
        // nothing to do with it.
        var (world, suit, _) = Board();
        var minion = world.CreateCard(
            "01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        Damage.Deal(world, Cards, minion, minion, 2, "test", "test", []);

        Assert.Equal(2, minion.Damage);
        Assert.Equal(0, suit.Damage);
    }

    [Rule("rr:damage.step.1")]
    [Fact]
    public void AnAbilityThatDoesNotTouchTheDamageLeavesItAlone()
    {
        // `rr:damage.step.1` holds abilities that *may* replace the damage, not
        // ones that must. One that watches and does something else must not
        // silently swallow it -- which is the difference between "this ability
        // said what is left" and "this ability ran".
        var book = Marvel.Cards.Dsl.AbilityCatalog.Parse(
            """
            {"cards":[{"card":"01098","abilities":[
              {"trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
               "effect":{"attachTo":{"query":"villain"}}},
              {"trigger":{"event":"WhenDamageWouldBeDealt","timing":"ForcedInterrupt",
                          "subject":"attachedTo"},
               "effect":{"giveStatus":{"card":"this","status":"tough"}}}]}]}
            """);

        var world = WorldSetup.DealWithoutCardAbilities(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        var runner = new Marvel.Cards.Run.AbilityRunner(book);
        world.Abilities = runner;

        var watcher = world.CreateCard(
            AuthoredCards.ArmoredSuit, world.AreaOf(DeckType.RevealingArea));
        runner.WhenRevealed(world, watcher, 0);
        var villain = world.TheCardIn(DeckType.VillainArea)!;

        Damage.Deal(world, Cards, villain, villain, 3, "test", "test", []);

        Assert.Equal(3, villain.Damage);
        Assert.Equal(0, watcher.Damage);
    }

    /// <summary>The Rhino board with the suit attached, by its own text.</summary>
    private static (World World, Card Suit, Card Villain) Board()
    {
        var world = WorldSetup.DealWithoutCardAbilities(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        world.Abilities = AuthoredCards.Runner();

        var suit = world.CreateCard(
            AuthoredCards.ArmoredSuit, world.AreaOf(DeckType.RevealingArea));
        AuthoredCards.Runner().WhenRevealed(world, suit, 0);

        return (world, suit, world.TheCardIn(DeckType.VillainArea)!);
    }
}
