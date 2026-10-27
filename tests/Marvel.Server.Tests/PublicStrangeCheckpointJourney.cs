using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Session;
using Xunit;
using static Marvel.Server.Tests.PublicDoctorStrangeRestartFixture;

namespace Marvel.Server.Tests;

/// <summary>Public legal starter journey; hidden state is observed, never prepared.</summary>
internal sealed class PublicStrangeCheckpointJourney
{
    internal PublicDoctorStrangeRestartFixture Factory { get; } = new();
    internal MemorySessionStore Store { get; } = new();
    internal EngineHost Host { get; }
    internal EngineResponse Opened { get; }
    internal EngineResponse Current { get; private set; }

    internal PublicStrangeCheckpointJourney(uint seed)
    {
        Host = new EngineHost(Factory, store: Store);
        Opened = Host.Exchange(EngineRequest.OpenGame("open", "strange-remaining-" + seed,
            new GameSpecification("rhino", ["doctor_strange"], [], seed)));
        Assert.Null(Opened.Error);
        Current = Answer(Host, Opened, Opened,
            Decision.Take(Assert.Single(Opened.Prompt!.Affordances).Id));
    }

    internal void Reach(Func<Game, EngineResponse, bool> checkpoint)
    {
        for (int index = 0; index < 100; index++)
        {
            if (checkpoint(Factory.Game, Current)) return;
            Assert.NotNull(Current.Prompt);
            Current = Answer(Host, Opened, Current, Choose(Current.Prompt, Factory.Game));
        }
        Assert.Fail("The pinned public starter journey did not reach its checkpoint in 100 decisions");
    }
    internal static Decision Choose(Prompt prompt, Game game)
    {
        var offers = prompt.Affordances.Where(offer => offer.Illegal is null).ToArray();
        if (!game.IsRootPrompt)
        {
            if (prompt.Cancellable) return Decision.Decline;
            return Select(offers[0]);
        }
        var identity = game.State.Seats[0].IdentityCard;
        var power = offers.FirstOrDefault(offer => offer.AnchorId == identity.ObjectId && offer.Verb == Game.ActionVerb);
        if (power is not null) return Select(power);
        var flip = offers.FirstOrDefault(offer => offer.Verb == Game.ChangeForm);
        if (flip is not null && (identity.FaceId == "09001b" || identity.Ready)) return Select(flip);
        var basic = offers.FirstOrDefault(offer => offer.Verb is "Thwart" or "Recover");
        if (basic is not null) return Select(basic);
        return prompt.Cancellable ? Decision.Decline : Select(offers.First(offer => offer.Verb == Game.EndPhaseVerb));
    }

    private static Decision Select(Affordance offer)
    {
        if (offer.CostOptions.Count > 0) return Paid(offer);
        return Decision.Take(offer.Id, offer.Targets?.Legal.Take(offer.Targets.Min).ToArray() ?? [], []);
    }

}
