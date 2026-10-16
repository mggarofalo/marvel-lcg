using Marvel.Rules.Setup;
using Marvel.Rules.State;

namespace Marvel.Content.Setup;

/// <summary>Admits complete product recipes before hosts advertise them.</summary>
public static class SetupCatalogAdmission
{
    /// <summary>Checks every referenced face, authored ability and selectable recipe.</summary>
    public static void Validate(SetupCatalog setup, CardCatalog cards, IReadOnlySet<string> authored)
    {
        ArgumentNullException.ThrowIfNull(setup);
        ArgumentNullException.ThrowIfNull(cards);
        ArgumentNullException.ThrowIfNull(authored);
        foreach (string name in setup.EncounterSetNames)
        {
            ValidateFaces(setup.EncounterSet(name), cards, authored);
            _ = ModularEncounterSets.IsModular(setup, cards, name);
        }
        foreach (string name in setup.HeroNames)
        {
            HeroSetup hero = setup.Hero(name);
            if (hero.Hero.Count != 1) throw new ArgumentException($"hero '{name}' requires one identity");
            ValidateFaces(hero.Hero.Concat(hero.HeroDeck).Concat(hero.PlayerDeck)
                .Concat(hero.Obligations).Concat(hero.NemesisSet), cards, authored);
            _ = IdentityFaces.StartingSpec(hero.Hero[0], cards);
            _ = PlayerDeckAdmission.Admit(new PlayerDeckRecipe([.. hero.HeroDeck, .. hero.PlayerDeck]), hero.HeroDeck, 0, cards);
        }
        foreach (string scenario in setup.CampaignNames)
        {
            CampaignSetup campaign = setup.Campaign(scenario);
            if (campaign.Schemes.Count == 0 || campaign.Villain.Count == 0)
                throw new ArgumentException($"scenario '{scenario}' requires a main scheme and villain");
            ValidateFaces(campaign.Villain.Concat(campaign.Schemes).Concat(campaign.Encounters)
                .Concat(campaign.SetAside).Concat(campaign.Challenges), cards, authored);
            foreach (string fixedSet in campaign.EncounterSets) _ = setup.EncounterSet(fixedSet);
            foreach (string modular in campaign.ModularSets)
                if (!ModularEncounterSets.IsModular(setup, cards, modular))
                    throw new ArgumentException($"scenario '{scenario}' recommends non-modular set '{modular}'");
            foreach (string hero in setup.HeroNames)
                _ = Blueprints.From(Dealer.DealOrder(setup, scenario, [hero], facts: cards), cards);
        }
    }

    /// <summary>Requires all supplied faces to belong to the admitted executable pool.</summary>
    public static void ValidateFaces(IEnumerable<string> specs, CardCatalog cards, IReadOnlySet<string> authored)
    {
        foreach (string spec in specs)
        {
            foreach (string face in spec.Split(','))
            {
                if (!cards.Has(face) || !authored.Contains(face))
                    throw new ArgumentException($"card '{face}' is outside the admitted executable content");
            }
        }
    }
}
