using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Bounded central Kang transition and its actual stage instructions.</summary>
public static class KangCentral
{
    internal static bool IsLastGroup(World world, KangDeparture departure) =>
        !world.GameAreas.Any(area => !ReferenceEquals(area, departure.Source) && Remains(world, area));

    /// <summary>Whether a game area other than central 2B is still on the table.</summary>
    /// <remarks>
    /// 11008b: "Players cannot join this game area unless there are no other game areas
    /// remaining." An eliminated player's local Kang and stage stay in their game area until
    /// that stage's printed phase-end removal (rr:player-elimination.step.5 removes only the
    /// player's own elements), so that area remains, and can be joined, until then.
    /// </remarks>
    internal static bool Remains(World world, GameArea area) =>
        area.PlayAreas.Any(play => play.IsPlayers) || world.Agenda.KangTransitions.Elimination.Holds(world, area);

    internal static void Preflight(World world, KangDeparture departure)
    {
        if (!IsLastGroup(world, departure)) return;
        RequireLastGroupShape(world, departure);
        var next = RequireCentralSchemes(world);
        RequireFinalVillain(world, departure, next);
        RequireNemesisAssociations(world);
    }

    private static void RequireLastGroupShape(World world, KangDeparture departure)
    {
        if (world.Players is < 1 or > 4 || world.Seats.Count != world.Players
            || departure.Group.Count != world.Seats.Count(seat => !seat.Eliminated)
            || world.Seats.Any(seat => !seat.Eliminated && !departure.Group.Contains(PlayArea.Of(seat.Index)))
            || departure.Group.Any(play => !play.IsPlayers)
            || world.GameAreaOf(PlayArea.Villains) is not null
            || world.GameAreas.Any(area => !ReferenceEquals(area, departure.Source) && area.PlayAreas.Count > 0))
            throw new RulesNotImplementedException("Kang central reunion requires one complete participating last group");
    }

    private static Card RequireCentralSchemes(World world)
    {
        var central = Require(world, "11008b", DeckType.MainSchemesArea);
        if (central.Area.PlayArea != PlayArea.Villains)
            throw new RulesNotImplementedException("Kang central reunion requires central 2B");
        var next = Require(world, "11013a", DeckType.MainSchemesDeck);
        if (next.Faces.Count != 2 || next.Faces[1] != "11013b"
            || world.Areas.Where(area => area.Host == next.ObjectId).SelectMany(area => area.Cards)
                .Any(card => card.FaceId != "11023" || card.Owner != -1 || card.FaceUp
                    || card.Area.Type != DeckType.BeneathCardsPile))
            throw new RulesNotImplementedException("Kang central reunion requires 4A with only concealed stored Dominions");
        return next;
    }

    private static void RequireFinalVillain(World world, KangDeparture departure, Card next)
    {
        var old = world.Cards[departure.Defeated.Card];
        string villainFace = old.FaceId is "11035" or "11036" or "11037" or "11038" ? "11039" : "11006";
        var villain = Require(world, villainFace, DeckType.AsideDeck);
        RequireFinalVillainAreas(world, departure, next, villain);
        if (world.Cards.Any(card => card.Area.Type == DeckType.AsideDeck
            && card.FaceId == (villainFace == "11006" ? "11039" : "11006")))
            throw new RulesNotImplementedException("Kang central reunion requires one matching mode");
    }

    private static void RequireFinalVillainAreas(World world, KangDeparture departure, Card next, Card villain)
    {
        if (next.Area.PlayArea != PlayArea.Villains || villain.Area.PlayArea != PlayArea.Villains
            || world.Areas.Any(area => area.Type == DeckType.VillainDeck && area.Cards.Count != 0)
            || world.Cards.Any(card => card.Area.Type == DeckType.VillainArea
                && !(departure.CompletedScheme is not null && card.ObjectId == departure.Defeated.Card)))
            throw new RulesNotImplementedException("Kang central reunion requires the matching set-aside final villain");
    }

    private static void RequireNemesisAssociations(World world)
    {
        foreach (var seat in world.Seats.Where(seat => !seat.Eliminated))
            if (seat.NemesisSet.Count(card => world.Facts.IsNemesisMinion(card.FaceId)) != 1)
                throw new RulesNotImplementedException("Kang central reunion requires each physical nemesis association");
    }

    internal static void Begin(World world, KangJoinOperation operation, List<GameEvent> events)
    {
        Preflight(world, operation.Departure);
        var old = Require(world, "11008b", DeckType.MainSchemesArea);
        var next = Require(world, "11013a", DeckType.MainSchemesDeck);
        long acceleration = old.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken);
        world.Agenda.KangTransitions.Elimination.Join(world, operation.Departure.Source,
            operation.Departure.Source, "11008b", events);
        world.Join(PlayArea.Villains, operation.Departure.Source, "11008b", events);
        KangFirstSplit.Remove(world, old, "11008b", events);
        Move(world, next, world.AreaOf(DeckType.MainSchemesArea), "11008b", events);
        next.PlaceTokens(EncounterDeck.AccelerationToken, acceleration);
        foreach (bool flip in new[] { false, true })
            world.Agenda.ContinueBeforeOwner(operation.Origin, operation.Owner,
                new PhaseStep(new KangCentralStageOperation(operation.Owner, operation.Origin, flip), operation.Owner.Round, operation.Owner.Number,
                    Subject: next.ObjectId, Seat: world.FirstPlayer, Plan: true));
    }

    /// <summary>Schedule the scenario instruction inside its owning reveal ability.</summary>
    public static void Schedule(World world, Card scheme, KangCentralPart part)
    {
        string expected = part == KangCentralPart.RevealVillain ? "11013a" : "11013b";
        if (scheme.FaceId != expected || scheme.Area.Type != DeckType.MainSchemesArea
            || world.Agenda.Current is not { Operation: KangCentralStageOperation stage } owner
            || owner.Subject != scheme.ObjectId || stage.Flip != (part == KangCentralPart.Nemeses)
            || !world.Agenda.IsOutstanding(stage.Owner, stage.Origin))
            throw new RulesNotImplementedException("Kang central instruction requires its retained phase-owned scheme reveal");
        world.Agenda.Now(new PhaseStep(new KangCentralOperation(part), owner.Round, owner.Number,
            Subject: scheme.ObjectId, Seat: world.FirstPlayer, Plan: true));
    }

    internal static Prompts.Prompt? Apply(World world, PhaseStep step, List<GameEvent> events)
    {
        if (step.Operation is KangCentralStageOperation stage)
        {
            if (!world.Agenda.IsOutstanding(stage.Owner, stage.Origin))
                throw new InvalidOperationException("the exact Kang phase owner is no longer outstanding");
            var scheme = world.Cards[step.Subject];
            if (stage.Flip)
            {
                scheme.TurnTo("11013b");
                events.Add(new CardsFlipped([scheme.ObjectId], true)
                    { SubjectFaces = OccurrenceFaces.For(scheme), Trigger = "11008b", Verb = "Advance" });
                Reveal.EnterPlay(world, world.Facts, scheme, events);
            }
            ActivityBatches.Append(events, world.EncounterAbilities.ExecuteReveal(world, scheme, step.Seat).Events);
            return null;
        }
        var op = step.Require<KangCentralOperation>();
        if (op.Part == KangCentralPart.RevealVillain)
        {
            var villain = world.Cards.Single(card => card.Area.Type == DeckType.AsideDeck
                && card.FaceId is "11006" or "11039");
            Move(world, villain, world.AreaOf(DeckType.VillainArea), "11013a", events);
            Reveal.EnterPlay(world, world.Facts, villain, events);
            // Printed 11013a reveals III, then each stored Dominion. Queue this
            // continuation before III's children so his complete reveal finishes first.
            world.Agenda.Now(step with { Operation = new KangCentralOperation(KangCentralPart.Dominions) });
            ActivityBatches.Append(events, world.EncounterAbilities.ExecuteReveal(world, villain, step.Seat).Events);
        }
        else if (op.Part == KangCentralPart.Dominions) KangDominionReveal.Apply(world, step, op, events);
        else SearchNext(world, step, op, events);
        return null;
    }

    private static void SearchNext(World world, PhaseStep step, KangCentralOperation operation, List<GameEvent> events)
    {
        var players = operation.Players ?? world.PlayerOrder.ToArray();
        if (operation.Index == players.Count)
        {
            var deck = world.AreaOf(DeckType.EncounterDeck);
            world.Shuffle(deck);
            events.Add(new AreaReordered(Places.Reference(deck), [.. deck.Cards.Select(card => card.ObjectId)])
                { Trigger = "11013b", Verb = "Shuffle" });
            return;
        }
        int player = players[operation.Index];
        var continuation = step with { Operation = operation with { Index = operation.Index + 1, Players = players } };
        // rr:player-elimination.6: "Effects that refer to the players in the game ignore eliminated players".
        if (world.Seats[player].Eliminated)
        {
            world.Agenda.Now(continuation);
            return;
        }
        var card = world.Seats[player].NemesisSet.Single(candidate => world.Facts.IsNemesisMinion(candidate.FaceId));
        if (card.Area.Type is DeckType.EncounterDeck or DeckType.EncounterDiscardPile or DeckType.AsideDeck)
        {
            EncounterEntry.Schedule(world, EncounterEntryRequest.For(card, player, new CardEntryPlacement(Verb: "Put_Into_Play")),
                new EffectCompletion(1, 1), continuation);
        }
        else world.Agenda.Now(continuation);
    }

    private static Card Require(World world, string face, DeckType area)
    {
        var cards = world.Cards.Where(card => card.FaceId == face && card.Area.Type == area).ToArray();
        return cards.Length == 1 ? cards[0]
            : throw new RulesNotImplementedException($"Kang central reunion requires one {face} in {area}");
    }

    private static void Move(World world, Card card, Area into, string trigger, List<GameEvent> events)
    {
        var from = card.Area;
        World.MoveToTop(card, into);
        card.TurnFaceUp();
        events.Add(new CardsMoved(Places.Reference(from), Places.Reference(into),
            [new Landing(card.ObjectId, into.Cards.Count - 1)]) { Trigger = trigger, Verb = "Advance" });
    }
}
