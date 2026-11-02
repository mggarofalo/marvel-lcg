using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic Ronan the Accuser boards over the production ability book.</summary>
internal static class RonanBoards
{
    internal const string RonanOne = "16103";
    internal const string RonanTwo = "16104";
    internal const string RonanThree = "16105";
    internal const string Interception = "16106a,16106b";
    internal const string TakeWhatIsMine = "16107a,16107b";
    internal const string CommandShip = "16108";
    internal const string UniversalWeapon = "16109";
    internal const string Fanaticism = "16110";
    internal const string CutThePower = "16111";
    internal const string PincerManeuver = "16112";
    internal const string SuperiorTactics = "16113";
    internal const string SingleMindedFury = "16114";
    internal const string KreePhysiology = "16115";
    internal const string YouStandAccused = "16116";

    /// <summary>
    /// Spider-Man in every seat (seat 0 on <paramref name="face"/>) against Ronan the Accuser
    /// (<paramref name="villain"/>) and <paramref name="scheme"/> on its B side. The encounter deck
    /// holds Hard to Keep Down, which prints no boost icon.
    /// </summary>
    internal static World Board(int players = 1, string face = "01001a", string villain = RonanOne,
        string scheme = Interception) =>
        BadoonBoards.Board(players, face, villain, scheme);

    /// <summary>
    /// Deals <paramref name="face"/> to <paramref name="player"/> and schedules its reveal from the
    /// encounter deck, as the villain phase does.
    /// </summary>
    internal static Card Reveal(World world, string face, int player = 0)
    {
        var card = world.CreateCard(face, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(player)));
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard,
                new EncounterRevealOrigin(card.ObjectId, Places.Reference(card.Area), Dealt: true)),
            Round: 1, Number: 4, Subject: card.ObjectId, Seat: player));
        return card;
    }

    /// <summary>The Kree Command Ship environment in play.</summary>
    internal static Card Ship(World world) => world.CreateCard(CommandShip, world.AreaOf(DeckType.EnvironmentArea));

    /// <summary>The Power Stone attached to <paramref name="host"/>.</summary>
    internal static Card Stone(World world, Card host) => ShipCommandBoards.Attach(world, host, ShipCommandBoards.PowerStone);

    /// <summary>The card <paramref name="card"/> is attached to.</summary>
    internal static Card Host(World world, Card card) => world.Cards[card.Area.Host];

    /// <summary>The boost cards an activation dealt, now in the encounter discard pile.</summary>
    internal static int Discarded(World world) => world.AreaOf(DeckType.EncounterDiscardPile).Cards.Count;

    /// <summary>A modified field of <paramref name="card"/>.</summary>
    internal static long Modified(World world, Card card, string field) =>
        StateFields.Modified(world, card, field, world.Facts, world.Players);

    /// <summary>
    /// Drains pending work as <see cref="BadoonBoards.Drive"/> does, taking the opportunity labelled
    /// <paramref name="label"/> paid with <paramref name="paying"/>; returns every prompt asked.
    /// </summary>
    internal static List<Prompt> Take(World world, string? label, params Card[] paying)
    {
        var asked = new List<Prompt>();
        BadoonBoards.Drive(world, prompt =>
        {
            asked.Add(prompt);
            return label is not null && prompt.Affordances.FirstOrDefault(a => a.Label == label) is { } taken
                ? Decision.Take(taken.Id, [], [.. paying.Select(card => card.ObjectId)])
                : null;
        });
        return asked;
    }
}
