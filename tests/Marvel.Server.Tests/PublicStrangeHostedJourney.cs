using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;
using Xunit;
using static Marvel.Server.Tests.PublicDoctorStrangeRestartFixture;

namespace Marvel.Server.Tests;

/// <summary>One pinned legal starter journey, using offers without preparing gameplay state.</summary>
internal static class PublicStrangeHostedJourney
{
    internal static bool HasStored(Game game) => BeneathCards.Of(game.State,
        game.State.Cards.Single(card => card.FaceId == "09029")).Count > 0;

    internal static Decision Choose(Game game, Prompt prompt)
    {
        var offers = prompt.Affordances.Where(offer => offer.Illegal is null).ToArray();
        if (game.Phase == GamePhase.Mulligan) return Decision.Take(Assert.Single(offers).Id);
        if (!game.IsRootPrompt)
            return prompt.Cancellable ? Decision.Decline : Select(offers[0]);
        var identity = game.State.Seats[prompt.Player].IdentityCard;
        var flip = offers.FirstOrDefault(offer => offer.Verb == Game.ChangeForm);
        if (flip is not null && identity.FaceId.EndsWith('b')) return Select(flip);
        if (HasStored(game)) return ClearScheme(game, prompt, offers);
        return End(prompt, offers);
    }

    private static Decision ClearScheme(Game game, Prompt prompt, Affordance[] offers)
    {
        var scheme = game.State.Cards.Single(card => card.FaceId == "09029");
        var astral = offers.FirstOrDefault(offer => offer.AnchorId >= 0 && game.State.Cards[offer.AnchorId].FaceId == "09003"
            && offer.Verb is "Play" or Game.ActionVerb);
        if (astral is not null) return Paid(astral) with { Targets = [scheme.ObjectId] };
        var thwart = offers.FirstOrDefault(offer => offer.Verb == "Thwart" && offer.Targets!.Legal.Contains(scheme.ObjectId));
        if (thwart is not null) return Decision.Take(thwart.Id, [scheme.ObjectId], []);
        return End(prompt, offers);
    }

    internal static void AssertPrivacy(Game game, bool stored, int scheme)
    {
        foreach (var scope in new[] { ViewScope.None, new RestrictedVisibilityPolicy(0).Authorize(null, 2),
                     new RestrictedVisibilityPolicy(1).Authorize(null, 2) })
        {
            var view = WorldProjection.For(game.State, game.Pending, [], scope).World;
            var deck = game.State.Seats[1].InvocationDeck!;
            var hidden = Assert.Single(view.Areas, area => area.Id == deck.Deck.Id).Cards;
            Assert.Equal(deck.Deck.Cards.Count, hidden.Count);
            Assert.Single(hidden, card => card.Face is not null && card.Id == deck.PublicTop!.ObjectId);
            Assert.All(hidden.Where(card => card.Face is null), card => Assert.Null(card.Id));
            if (stored)
            {
                var beneath = Assert.Single(view.Areas, area => area.Host == scheme && area.Zone == "BeneathCardsPile");
                var concealed = Assert.Single(beneath.Cards);
                Assert.Null(concealed.Face);
                Assert.Null(concealed.Id);
                Assert.False(concealed.FaceUp);
            }
        }
    }

    private static Decision End(Prompt prompt, Affordance[] offers) =>
        prompt.Cancellable ? Decision.Decline : Select(offers.First(offer => offer.Verb == Game.EndPhaseVerb));

    private static Decision Select(Affordance offer) =>
        Decision.Take(offer.Id, offer.Targets?.Legal.Take(offer.Targets.Min).ToArray() ?? [], []);
}

