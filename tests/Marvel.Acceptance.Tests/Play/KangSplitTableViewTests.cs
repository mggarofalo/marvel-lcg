using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Server;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

// Kang stays outside public admission; KangRecipeFactory deals the declared recipe through
// the production dealer behind the host's game factory seam. Each seat holds its own
// restricted capability, and every response crosses the strict wire codec.
public sealed class KangSplitTableViewTests
{
    [Fact]
    [Rule("rr:first-player.3")]
    [Rule("rr:per-player-icon")]
    public void EachSeatSeesTheSplitItsDepartureAndTheReunionWithoutTheOtherSeatsHand()
    {
        // pack:mc11:game-areas: "Cards and components in one game area cannot affect another
        // game area (with the exception of the text on stage 2B)." 11002-11005: a defeated
        // Kang (II)'s group joins another game area at the end of the phase; 11013a reunites
        // the table against Kang (III).
        var factory = new KangRecipeFactory();
        var host = new EngineHost(factory, visibility: new RestrictedVisibilityPolicy(0));
        var opened = host.Exchange(EngineRequest.OpenGame("open", Game, new GameSpecification(
            "kang", KangRecipeGames.Heroes.Take(2).ToArray(), ["temporal"], 1)));
        var attached = host.Exchange(EngineRequest.AttachGame("attach", Game, Assert.Single(opened.Invitations!).Invitation));
        string[] capabilities = [opened.Capability!, attached.Capability!];
        var policy = new CoreGamePolicy(factory.Game.State.Facts);
        bool split = false, departing = false, reunited = false;
        long revision = opened.Revision;

        while (factory.Game.Pending is { } prompt && !reunited)
        {
            Assert.True(policy.Answered < 3000);
            var answer = KangRecipeGames.Answer(policy, factory.Game);
            // The host authorizes the seat an offer acts for, which an off-turn Action can differ from.
            int actor = answer.IsDecline ? prompt.Player
                : DecisionAssessment.ActingSeat(prompt, prompt.Affordances.Single(option => option.Id == answer.Affordance));
            var answered = host.Exchange(EngineRequest.ResolveGame($"answer-{policy.Answered}", Game,
                capabilities[actor], answer, revision));
            Assert.Null(answered.Error);
            revision = answered.Revision;
            var views = Enumerable.Range(0, 2).Select(seat => Wire(host.Exchange(
                EngineRequest.SyncGame($"sync-{seat}-{policy.Answered}", Game, capabilities[seat])))).ToArray();
            Observe(factory.Game.State, views, ref split, ref departing, ref reunited);
        }

        Assert.True(split && departing && reunited);
    }

    private static void Observe(World world, WorldDescriptor[] views, ref bool split, ref bool departing, ref bool reunited)
    {
        for (int seat = 0; seat < 2; seat++) Private(world, views[seat], seat);
        Assert.Equal(Public(views[0]), Public(views[1]));
        var kang = Assert.IsType<KangTableDescriptor>(views[0].Kang);
        if (Places.Separated(world))
        {
            split = true;
            Separated(world, views, kang);
        }
        if (world.Agenda.KangTransitions.Pending.Count > 0)
        {
            departing = true;
            Departing(world, kang);
        }
        if (split && kang.Stage == 3)
        {
            reunited = true;
            Reunited(world, views, kang);
        }
    }

    private static void Reunited(World world, WorldDescriptor[] views, KangTableDescriptor kang)
    {
        var area = Assert.Single(views[0].GameAreas, item => item.PlayAreas.Count > 0);
        Assert.True(area.Central);
        Assert.Contains(world.Cards.Single(card => KangTable.IsThirdStage(card)
            && card.Area.Type == DeckType.VillainArea).ObjectId, area.Villains);
        Assert.Equal("Defeat Kang (III) to win.", kang.Progress);
        Assert.Empty(kang.Notes);
        Assert.All(views, view => Assert.All(view.Players, player => Assert.Empty(player.OutOfReach)));
    }

    private static void Separated(World world, WorldDescriptor[] views, KangTableDescriptor kang)
    {
        Assert.Equal(2, kang.Stage);
        Assert.Equal(2, kang.StartingPlayers);
        Assert.Equal(5, kang.Notes.Count);
        Assert.Equal([Assert.Single(world.AreaOf(DeckType.MainSchemesArea).Cards).ObjectId], kang.CentralSchemes);
        for (int seat = 0; seat < 2; seat++)
        {
            if (world.Seats[seat].Eliminated) continue;
            var area = Assert.Single(views[seat].GameAreas, item => item.PlayAreas.Contains(seat));
            Assert.False(area.Central);
            Assert.Contains(world.Seats[seat].Name, area.Label, StringComparison.Ordinal);
            Assert.DoesNotContain("game area " + area.Id, area.Label, StringComparison.Ordinal);
            var engine = world.GameAreaOf(PlayArea.Of(seat))!;
            Assert.Equal(engine.Id, views[seat].Players[seat].GameArea);
            // Another area's villains are out of this seat's reach; its own never are.
            foreach (var other in views[seat].GameAreas.Where(item => item.Id != engine.Id))
                Assert.All(other.Villains, id => Assert.Contains(id, views[seat].Players[seat].OutOfReach));
            Assert.All(area.Villains, id => Assert.DoesNotContain(id, views[seat].Players[seat].OutOfReach));
            Assert.All(kang.CentralSchemes, id => Assert.DoesNotContain(id, views[seat].Players[seat].OutOfReach));
        }
    }

    private static void Departing(World world, KangTableDescriptor kang)
    {
        var pending = world.Agenda.KangTransitions.Pending;
        Assert.Equal(pending.Select(item => item.Defeated.Card).Order(), kang.Departures.Select(item => item.Villain));
        foreach (var departure in kang.Departures)
        {
            var engine = pending.Single(item => item.Defeated.Card == departure.Villain);
            Assert.Equal(engine.Source.Id, departure.SourceGameArea);
            Assert.Equal(engine.CompletedScheme?.Card, departure.CompletedScheme);
            Assert.Contains(departure.CompletedScheme is null ? "was defeated" : "storing a Kang's Dominion",
                departure.Summary, StringComparison.Ordinal);
            Assert.Equal(departure.Destinations.Count == 0, departure.Reunion);
        }
    }

    // A seat reads its own hand and never another seat's.
    private static void Private(World world, WorldDescriptor view, int seat)
    {
        foreach (var hand in view.Areas.Where(area => area.Zone == nameof(DeckType.HandsArea) && area.Owner >= 0))
            Assert.All(hand.Cards, card => Assert.Equal(hand.Owner == seat, card.Face is not null));
        int[] hidden = [.. world.Seats.Where(other => other.Index != seat).SelectMany(other => other.Hand.Cards)
            .Select(card => card.ObjectId)];
        Assert.DoesNotContain(view.Players.SelectMany(player => player.OutOfReach), hidden.Contains);
        Assert.DoesNotContain(view.GameAreas.SelectMany(area => area.Villains.Concat(area.MainSchemes)), hidden.Contains);
    }

    private static string Public(WorldDescriptor view) => System.Text.Json.JsonSerializer.Serialize(new
    {
        view.GameAreas,
        Reach = view.Players.Select(player => player.OutOfReach),
        view.Kang,
    });

    // The strict codec rejects unknown members; the bytes it writes must read back to the same bytes.
    private static WorldDescriptor Wire(EngineResponse response)
    {
        Assert.Null(response.Error);
        byte[] bytes = EngineJson.Write(response);
        var read = EngineJson.ReadResponse(bytes);
        Assert.Equal(bytes, EngineJson.Write(read));
        return read.World!;
    }

    private const string Game = "kang-view";
}
