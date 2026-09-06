using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Environments, which the engine had no card type for.
/// </summary>
/// <remarks>
/// <para>
/// <c>rr:environment</c>: "an environment card enters play in the villain's
/// play area, and is <b>active so long as it remains in play</b>."
/// <c>rr:reveal.2</c> gives it that destination when it is revealed.
/// </para>
/// <para>
/// <b>The failure was not that the type was missing.</b>
/// <c>CardCatalog.ToKind</c> had no <c>Environment</c>, so all eighty
/// environments in the pool answered <c>Unknown</c> — and an unknown card is
/// one <c>rr:reveal.7</c> leaves on the table in front of the player, which
/// step 4 then discards. A card whose whole purpose is to stay in play was
/// revealed and thrown away, and the board it left behind was plausible.
/// </para>
/// </remarks>
public sealed class EnvironmentTests
{
    /// <summary>"Ultron Drones", an environment of the Ultron set.</summary>
    private const string UltronDrones = "01140";

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:environment")]
    [Fact]
    public void AnEnvironmentIsAKindTheEngineKnows()
    {
        // The half that is data. Eighty cards in the pool are environments and
        // every one of them read as `Unknown`, which is the answer for a card
        // the extract could not type at all.
        Assert.Equal(CardKind.Environment, Cards.Kind(UltronDrones));
    }

    [Rule("rr:reveal.2")]
    [Rule("rr:environment.1")]
    [Rule("rr:villain-s-play-area.1")]
    [Fact]
    public void ARevealedEnvironmentEntersPlayAndStaysThere()
    {
        // "**Environment**: It enters play in the villain's play area." It
        // then "remains in play until a card ability or game effect causes it
        // to leave play": step 4 discards a card left in the revealing area,
        // so the whole test is that the card is not there when the step ends.
        var world = Deal();
        var card = world.CreateCard(
            UltronDrones, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)));

        world.Agenda.Add(new PhaseStep(
            Steps.RevealEncounterCard, 1, 4, Subject: card.ObjectId, Seat: 0));
        Run(world);

        Assert.Equal(DeckType.EnvironmentArea, card.Area.Type);
        Assert.True(card.Area.PlayArea.IsVillains, "the villain's play area, not a player's");
    }

    /// <summary>
    /// Runs the agenda out with no card abilities at all.
    /// </summary>
    /// <remarks>
    /// <c>NoCardAbilities</c> rather than the real interpreter, because
    /// <c>01140</c> is not authored and a revealed card nobody has written
    /// throws — which is correct, and is not what this is about. What is under
    /// test is where <c>rr:reveal.2</c> puts an environment, which is a step of
    /// the rules and happens before any ability runs.
    /// </remarks>
    private static void Run(World world)
    {
        var abilities = new NoCardAbilities();
        var events = new List<GameEvent>();
        var asked = Sequence.Work(world, Cards, abilities, events);
        for (int answered = 0; asked is not null; answered++)
        {
            Assert.True(answered < 12, $"'{asked.Label}' is still being asked");
            Sequence.Answer(
                world, Cards, abilities, asked,
                asked.Cancellable ? Decision.Decline : Decision.Take(asked.Affordances[0].Id),
                events);
            asked = Sequence.Work(world, Cards, abilities, events);
        }
    }

    private static World Deal() => WorldSetup.DealWithoutCardAbilities(
        Cards,
        Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
        ["Spider-Man"],
        12345);
}
