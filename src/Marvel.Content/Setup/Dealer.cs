using Marvel.Rules.Setup;
using Marvel.Rules.State;

namespace Marvel.Content.Setup;

/// <summary>
/// The order the engine creates cards in, which is the <c>object_id</c> contract.
/// </summary>
/// <remarks>
/// <para>
/// A card's <c>object_id</c> is its position in this sequence, and
/// <c>object_id</c> is on the wire in every state digest — checklist item 1 of
/// <c>docs/state-digest-v3.md</c>, <i>"everything else depends on this"</i>. So
/// this is a wire format and not a convenience.
/// </para>
/// <para>
/// <c>SetupDealTests</c> holds it, against <c>rr:appendix-ii-setup</c> for where a
/// card ends up and against this contract for which id it is given. The two are
/// separate claims: the rulebook orders the <i>steps</i> and says nothing about
/// ids, so a test that cited a rule for the allocation would be misreading one.
/// </para>
/// <para>
/// Two things it does not do, because neither affects allocation and both
/// belong to the step after it: it does not shuffle, and it does not say where
/// a card ends up. An obligation is <i>created</i> into its player's nemesis
/// pile and <i>moved</i> onto the encounter deck before the shuffle; both are
/// true and only the first one is an id.
/// </para>
/// </remarks>
public static class Dealer
{
    /// <summary>Every card the engine creates during setup, in allocation order.</summary>
    /// <param name="catalog">The setup dataset.</param>
    /// <param name="campaignName">The scenario, by dataset name.</param>
    /// <param name="heroNames">The heroes, in seat order.</param>
    /// <param name="modularSetNames">
    /// The chosen modular sets, or null to use the scenario's recommended sets.
    /// </param>
    /// <param name="facts">
    /// Printed facts used to reject a Standard or Expert set selected as a
    /// modular set. Null preserves the data-only deal-order operation.
    /// </param>
    /// <param name="playerDeckNames">
    /// Optional published starter names whose 25-card customization blocks
    /// replace the selected heroes' blocks, one per seat.
    /// </param>
    /// <param name="playerDecks">Explicit full decks, one per seat; null uses published decks.</param>
    /// <exception cref="KeyNotFoundException">A name the dataset does not hold.</exception>
    public static IReadOnlyList<Creation> DealOrder(
        SetupCatalog catalog, string campaignName, IReadOnlyList<string> heroNames,
        IReadOnlyList<string>? modularSetNames = null, ICardFacts? facts = null,
        IReadOnlyList<string>? playerDeckNames = null, IReadOnlyList<PlayerDeckRecipe>? playerDecks = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(heroNames);
        ValidateDeckSelections(heroNames, facts, playerDeckNames, playerDecks);

        var campaign = catalog.Campaign(campaignName);
        if (facts is not null)
        {
            ValidateModularSets(catalog, facts, modularSetNames ?? campaign.ModularSets);
        }
        var dealt = new List<Creation>
        {
            new("rule_a,rule_b", CreationSource.Rules, Creation.Scenario),
        };

        foreach (var challenge in campaign.Challenges)
        {
            dealt.Add(new Creation(challenge, CreationSource.Challenge, Creation.Scenario));
        }

        for (int seat = 0; seat < heroNames.Count; seat++)
        {
            var hero = catalog.Hero(heroNames[seat]);
            // The rulebook decides whether the resulting deck is legal;
            // choosing a published starter's 25-card customization block as a
            // compact deterministic deck recipe is this engine's test format.
            var customization = playerDeckNames is null
                ? hero
                : catalog.Hero(playerDeckNames[seat]);
            Add(hero.Hero, CreationSource.Identity, seat);
            Add(hero.Obligations, CreationSource.Obligation, seat);
            foreach (string spec in hero.NemesisSet)
                dealt.Add(new(spec, CreationSource.Nemesis, seat, hero.NemesisSetIcon));

            // One call over the concatenation in the engine, so the two lists
            // are a single unbroken run of ids rather than two.
            if (playerDecks is not null)
                dealt.AddRange(PlayerDeckAdmission.Admit(playerDecks[seat], hero.HeroDeck, seat, facts!));
            else
            {
                Add(hero.HeroDeck, CreationSource.HeroDeck, seat);
                Add(customization.PlayerDeck, CreationSource.PlayerDeck, seat);
            }
        }

        Add(campaign.Schemes, CreationSource.MainScheme, Creation.Scenario);
        Add(campaign.Villain, CreationSource.Villain, Creation.Scenario);
        Add(campaign.Encounters, CreationSource.Encounter, Creation.Scenario);

        // The printed contents line says which cards begin set aside, not when
        // an engine allocates their object ids. This engine chooses one stable
        // place: after the scenario's own encounter cards and before named
        // encounter sets.
        Add(campaign.SetAside, CreationSource.ScenarioSetAside, Creation.Scenario);

        foreach (var setName in EncounterSetNames(campaign, modularSetNames))
        {
            Add(catalog.EncounterSet(setName), CreationSource.EncounterSet, Creation.Scenario);
        }

        if (facts is not null)
        {
            DeckConstruction.Validate(dealt, facts);
        }

        return dealt;

        void Add(IEnumerable<string> specs, CreationSource source, int player)
        {
            foreach (var spec in specs)
            {
                dealt.Add(new Creation(spec, source, player));
            }
        }
    }

    private static void ValidateDeckSelections(
        IReadOnlyList<string> heroNames, ICardFacts? facts,
        IReadOnlyList<string>? playerDeckNames, IReadOnlyList<PlayerDeckRecipe>? playerDecks)
    {
        if (playerDecks is not null
            && (playerDecks.Count != heroNames.Count || playerDeckNames is not null || facts is null))
            throw new ArgumentException("explicit decks require facts and one recipe per seat, without starter substitutions", nameof(playerDecks));
        if (playerDeckNames is not null && playerDeckNames.Count != heroNames.Count)
        {
            throw new ArgumentException(
                "a customization source must be named for every selected hero",
                nameof(playerDeckNames));
        }
    }

    private static void ValidateModularSets(
        SetupCatalog catalog, ICardFacts facts, IReadOnlyList<string> selected)
    {
        if (selected.Distinct(StringComparer.Ordinal).Count() != selected.Count)
            throw new ArgumentException("a modular encounter set may be selected only once", nameof(selected));
        foreach (string name in selected)
        {
            if (!ModularEncounterSets.IsModular(catalog, facts, name))
            {
                // `rr:standard-set.1` and `rr:expert-set.1`: neither set is a
                // modular encounter set and neither can be selected as one.
                throw new ArgumentException(
                    $"encounter set '{name}' is not a modular set",
                    nameof(selected));
            }
        }
    }

    /// <summary>The named sets that go into the encounter deck, in order.</summary>
    /// <remarks>
    /// <para>
    /// <c>modular_sets</c> is appended to <c>encounter_sets</c> when the caller
    /// names no sets of its own. The dataset keeps the two apart so that a
    /// scenario played with chosen modulars stays expressible.
    /// </para>
    /// <para>
    /// Null and an empty list are different on purpose. Null asks for the
    /// printed recommendation; an empty list asks for no modular set. The
    /// rules permit either choice, and the API must not turn one into the
    /// other.
    /// </para>
    /// </remarks>
    /// <param name="campaign">The scenario.</param>
    /// <param name="modularSetNames">
    /// The chosen modular sets, or null to use the scenario's recommended sets.
    /// </param>
    public static IReadOnlyList<string> EncounterSetNames(
        CampaignSetup campaign, IReadOnlyList<string>? modularSetNames = null)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return [.. campaign.EncounterSets, .. (modularSetNames ?? campaign.ModularSets)];
    }

}
