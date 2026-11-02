using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Derives Kang's public progress facts from the engine's own state.</summary>
/// <remarks>
/// The scenario is won by defeating Kang (III) after the reunion (11013b); 11001
/// splits the table at Kang (I)'s defeat, and each Kang (II)'s defeat or completed
/// stage 3B moves its group at the end of the phase (11002–11005, 11009b–11012b).
/// Every value here reads the engine's captures (<see cref="KangFirstSplit"/>,
/// <see cref="KangPhaseTransitions"/>) or the cards in play; none is a forecast.
/// </remarks>
public static class KangTable
{
    private const string StageFour = "11013a";
    private const string Dominion = "11023";

    /// <summary>Whether a physical face is Kang (III).</summary>
    public static bool IsThirdStage(Card card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return card.FaceId is "11006" or "11039";
    }

    /// <summary>The facts for a Kang game, or null when no Kang villain is in this game.</summary>
    public static KangTableFacts? Of(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!world.Cards.Any(card => KangFirstSplit.IsFirstStage(card) || KangPhaseTransitions.IsSecondStage(card)
            || IsThirdStage(card)))
            return null;
        var pending = world.Agenda.KangTransitions.Pending;
        var villains = world.Cards.Where(card => card.Area.Type == DeckType.VillainArea).ToArray();
        int[] central = [.. world.Cards.Where(card => card.Area.Type == DeckType.MainSchemesArea
            && Places.GameAreaOf(world, card) is null).Select(card => card.ObjectId).Order()];
        return new(Stage(world, villains), Remaining(villains, pending), StoredDominions(world), central,
            world.Agenda.KangSplit.Pending?.Card,
            [.. pending.OrderBy(item => item.Defeated.Card).Select(item => Describe(world, item))],
            VillainPhaseFocus.Of(world))
        {
            Notes = Notes(world, central), LocalKangsDealt = world.Agenda.KangSplit.LocalKangsDealt,
        };
    }

    // Each note applies exactly while the table is separated. A stage already completed
    // has stored its Dominion, so only stages still in play and uncompleted carry that note.
    private static KangNote[] Notes(World world, int[] central)
    {
        if (!Places.Separated(world)) return [];
        KangNote[] table =
        [
            new(KangNoteKind.AreaIsolation, null), new(KangNoteKind.StepsInTurnOrder, null),
            new(KangNoteKind.EachPlayerIsArea, null), new(KangNoteKind.SharedEncounterDeck, null),
        ];
        var stages = world.Cards.Where(card => KangLocalCompletion.IsLocal(card)
                && card.Area.Type == DeckType.MainSchemesArea && card.Tokens.GetValueOrDefault("is_completed") != 1)
            .Select(card => new KangNote(KangNoteKind.CompletionStoresDominion, card.ObjectId));
        return [.. table, .. central.Select(id => new KangNote(KangNoteKind.CentralTokensWaitFor4A, id)), .. stages];
    }

    private static int Stage(World world, Card[] villains)
    {
        if (villains.Any(IsThirdStage)) return 3;
        return Places.Separated(world) || villains.Any(KangPhaseTransitions.IsSecondStage) ? 2 : 1;
    }

    // A completed stage's Kang (II) stays in play until his departure, which already counts him.
    private static int Remaining(Card[] villains, IReadOnlyList<KangDeparture> pending) =>
        villains.Count(card => KangPhaseTransitions.IsSecondStage(card)
            && !pending.Any(item => item.Defeated.Card == card.ObjectId && item.Defeated.Incarnation == card.Incarnation));

    // 11009b–11012b store each Dominion "facedown under stage 4A". The pile's size is
    // public although its cards are facedown. Both filters are equivalent mutants today:
    // only KangLocalCompletion stores beneath 4A, and it stores nothing but Dominions there.
    private static int StoredDominions(World world) => world.Areas
        .Where(area => area.Type == DeckType.BeneathCardsPile && area.Host >= 0
            && world.Cards[area.Host].Faces[0] == StageFour)
        .Sum(area => area.Cards.Count(card => card.FaceId == Dominion));

    private static KangPendingDeparture Describe(World world, KangDeparture departure)
    {
        var destinations = KangJoinProcedure.Destinations(world, departure);
        return new(departure.Defeated.Card, departure.Source.Id,
            [.. departure.Group.Where(area => area.IsPlayers).Select(area => area.Player)],
            departure.CompletedScheme?.Card, [.. destinations.Select(area => area.Id)], destinations.Length == 0);
    }
}
