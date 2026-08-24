namespace Marvel.Content.Setup;

/// <summary>
/// The order the engine creates cards in, which is the <c>object_id</c> contract.
/// </summary>
/// <remarks>
/// <para>
/// A card's <c>object_id</c> is its position in this sequence, and
/// <c>object_id</c> is on the wire in every state digest — checklist item 1 of
/// <c>docs/state-digest-v2.md</c>, <i>"everything else depends on this"</i>. So
/// this is a wire format and not a convenience.
/// </para>
/// <para>
/// Read out of the Python engine rather than invented — <c>RegisterPlayRule</c>,
/// <c>PlayerSetup.SelectIdentity</c>, <c>World.SelectScenario</c> and
/// <c>World.Initialize</c> — and held against a recorded game:
/// <c>datasets/digest/vectors.json</c> names the card at every id for
/// <c>rhino / spider_man / 12345</c> and all 81 agree. The Python mirror of this
/// is <c>py_src/tools/setup/deal.py</c>; the two are deliberately the same
/// shape so a divergence is a diff rather than an argument.
/// </para>
/// <para>
/// Two things it does not do, because neither affects allocation and both
/// belong to the step after it: it does not shuffle, and it does not say where
/// a card ends up. An obligation is <i>created</i> into its player's nemesis
/// pile and <i>moved</i> onto the encounter deck before the shuffle; both are
/// true and only the first one is an id.
/// </para>
/// <para>
/// <b>Not covered yet.</b> Measured over 48 boards, 38 reproduce this exactly.
/// The rest need one of three things, and only the first is data: linked cards
/// (a <c>Linked</c> attribute in <c>datasets/cards/</c>, which allocates the
/// linked card <i>before</i> the card naming it), setup abilities on a card, and
/// status cards allocated mid-setup. See <c>docs/setup-dataset.md</c>.
/// </para>
/// </remarks>
public static class Dealer
{
    /// <summary>Every card the engine creates during setup, in allocation order.</summary>
    /// <param name="catalog">The setup dataset.</param>
    /// <param name="campaignName">The scenario, by dataset name.</param>
    /// <param name="heroNames">The heroes, in seat order.</param>
    /// <exception cref="KeyNotFoundException">A name the dataset does not hold.</exception>
    public static IReadOnlyList<Creation> DealOrder(
        SetupCatalog catalog, string campaignName, IReadOnlyList<string> heroNames)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(heroNames);

        var campaign = catalog.Campaign(campaignName);
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
            Add(hero.Hero.Select(MoveBToFront), CreationSource.Identity, seat);
            Add(hero.Obligations, CreationSource.Obligation, seat);
            Add(hero.NemesisSet, CreationSource.Nemesis, seat);

            // One call over the concatenation in the engine, so the two lists
            // are a single unbroken run of ids rather than two.
            Add(hero.HeroDeck, CreationSource.HeroDeck, seat);
            Add(hero.PlayerDeck, CreationSource.PlayerDeck, seat);
        }

        Add(campaign.Schemes, CreationSource.MainScheme, Creation.Scenario);
        Add(campaign.Villain, CreationSource.Villain, Creation.Scenario);
        Add(campaign.Encounters, CreationSource.Encounter, Creation.Scenario);

        foreach (var setName in EncounterSetNames(campaign))
        {
            Add(catalog.EncounterSet(setName), CreationSource.EncounterSet, Creation.Scenario);
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

    /// <summary>The named sets that go into the encounter deck, in order.</summary>
    /// <remarks>
    /// The Python scene loader appends <c>modular_sets</c> to
    /// <c>encounter_sets</c> <b>only when the caller names no sets of its own</b>.
    /// The dataset keeps the two apart so that the other case — a scenario played
    /// with chosen modulars — stays expressible; this joins them for the default.
    /// </remarks>
    /// <param name="campaign">The scenario.</param>
    public static IReadOnlyList<string> EncounterSetNames(CampaignSetup campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return [.. campaign.EncounterSets, .. campaign.ModularSets];
    }

    /// <summary>
    /// Puts the alter-ego face first, as the engine's <c>move_b_to_front</c> does.
    /// </summary>
    /// <remarks>
    /// An identity is printed <c>a,b</c> — hero side, alter-ego side — and the
    /// game begins in alter-ego form. The engine reorders the spec rather than
    /// flipping the card afterwards, which is why the digest's <c>card</c> for a
    /// hero at step 0 is the <c>b</c> id. Faces that do not end in <c>b</c> keep
    /// their printed order.
    /// </remarks>
    /// <param name="spec">A comma-separated face list.</param>
    public static string MoveBToFront(string spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var faces = spec.Split(',');
        return string.Join(
            ',',
            faces.Where(face => face.EndsWith('b')).Concat(faces.Where(face => !face.EndsWith('b'))));
    }
}
