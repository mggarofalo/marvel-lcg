using Marvel.Rules.Setup;
using Marvel.Rules.State;

namespace Marvel.Content.Setup;

/// <summary>Allocates each seat's identity and validated deck, including scenario-permitted encounter cards.</summary>
internal static class HeroRecipeDeal
{
    internal static void Append(SetupCatalog catalog, DealRequest request, ICardFacts? facts,
        CampaignSetup campaign, List<Creation> dealt)
    {
        // pack:mc03:additional-setup.prepare-encounter-decks excludes both hero
        // obligations and nemesis sets from independent encounter groups. A
        // recipe that removes hero obligations deals them into the
        // removed-from-game area, where a card can still find them, and keeps
        // each nemesis set aside.
        bool nemeses = campaign.EncounterGroups is null;
        var obligation = campaign.RemovesHeroObligations ? CreationSource.RemovedObligation : CreationSource.Obligation;
        for (int seat = 0; seat < request.Heroes.Count; seat++)
        {
            var hero = CampaignDeckExclusions.Apply(catalog.Hero(request.Heroes[seat]), request.CampaignExcludedAllies);
            Add(dealt, hero.Hero, CreationSource.Identity, seat);
            if (nemeses) Add(dealt, hero.Obligations, obligation, seat);
            if (nemeses)
                foreach (string spec in hero.NemesisSet)
                    dealt.Add(new(spec, CreationSource.Nemesis, seat, hero.NemesisSetIcon));
            AppendDeck(catalog, request, facts, dealt, hero, seat);
            // Engine allocation choice: each seat's Invocations follow its ordinary deck.
            if (hero.InvocationDeck is { } invocation)
                Add(dealt, invocation.Cards, CreationSource.InvocationDeck, seat);
        }
    }

    private static void AppendDeck(SetupCatalog catalog, DealRequest request, ICardFacts? facts,
        List<Creation> dealt, HeroSetup hero, int seat)
    {
        // The rulebook decides deck legality. Choosing a published customization
        // block as a compact deterministic recipe is this engine's input format.
        if (request.PlayerDecks is not null)
        {
            dealt.AddRange(PlayerDeckAdmission.Admit(request.PlayerDecks[seat], hero.HeroDeck,
                seat, facts!, hero.Hero[0], hero.AspectRule));
            return;
        }
        var customization = request.StarterDecks is null ? hero : catalog.Hero(request.StarterDecks[seat]);
        if (facts is not null)
            _ = PlayerDeckAdmission.Admit(new([.. hero.HeroDeck, .. customization.PlayerDeck]),
                hero.HeroDeck, seat, facts, hero.Hero[0], hero.AspectRule);
        Add(dealt, hero.HeroDeck, CreationSource.HeroDeck, seat);
        Add(dealt, customization.PlayerDeck, CreationSource.PlayerDeck, seat);
    }

    private static void Add(List<Creation> dealt, IEnumerable<string> specs, CreationSource source, int seat)
    {
        foreach (string spec in specs) dealt.Add(new(spec, source, seat));
    }
}
