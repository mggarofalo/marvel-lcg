using System.Text.Json;
using Marvel.Content.Setup;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.State;

/// <summary>
/// Forms, held against the printed pool and a real dealt board.
/// </summary>
/// <remarks>
/// <para>
/// <c>FormsTests</c> checks the rules on made-up cards. This checks the two
/// claims that can only be made about real data: that the "[type] form" keyword
/// is read off exactly the faces that print it, and that a hero face registers
/// the fields a hero prints.
/// </para>
/// </remarks>
public sealed class FormDataTests
{
    private const string Campaign = "rhino";
    private const uint Seed = 12345;

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    private static readonly string CardText =
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json"));

    /// <summary>Every face in the pool that grants an additional form.</summary>
    /// <remarks>
    /// Nine, and the whole list fits here. Three of Spectrum's energy forms,
    /// two each of Vision's and Shadowcat's mass forms, two of Nick Fury's suit
    /// forms. <b>Every one is a permanent upgrade on its own card</b> — not one
    /// is a face of an identity, which is the measurement behind
    /// <c>Forms.Of</c> reading the play area as well as the identity.
    /// </remarks>
    private static readonly (string Face, string Form)[] Granting =
    [
        ("21002", "energy"),   // Gamma
        ("21003", "energy"),   // Photon
        ("21004", "energy"),   // Pulsar
        ("26002a", "mass"),    // Intangible
        ("26002b", "mass"),    // Dense
        ("32031a", "mass"),    // Solid
        ("32031b", "mass"),    // Phased
        ("50035a", "suit"),    // Assault
        ("50035b", "suit"),    // Stealth
        ("57046a", "mass"),    // Colossal
        ("57046b", "mass"),    // Miniature
    ];

    [Rule("rr:form-change-form.6")]
    [Fact]
    public void TheFormKeywordIsOnExactlyTheFacesThatPrintIt()
    {
        // Both directions. A test that only checked the nine would pass while
        // the reader marked half the pool, and that is the failure mode a text
        // scan actually has.
        var found = new List<(string Face, string Form)>();
        using var document = JsonDocument.Parse(CardText);
        foreach (var element in document.RootElement.GetProperty("cards").EnumerateArray())
        {
            string id = element.GetProperty("card_id").GetString()!;
            if (Cards.FormKeyword(id) is { } form)
            {
                found.Add((id, form));
            }
        }

        Assert.Equal(Granting, found);
    }

    [Rule("rr:form-change-form.6")]
    [Theory]
    // The keyword is a sentence of its own, and these are the three ways a
    // form can be *named* without being granted.
    //
    // `42024` Apocalyptic Influence, an obligation, tests whether you are in a
    // form; `32031a` Solid describes attacking in one; `rr:form-change-form.7`
    // gates playing a card on one. None of the three grants anything, and all
    // three would be swept up by a scan for the words "<Type> form".
    [InlineData("When Revealed: If you are in Archangel form, place 2 threat.")]
    [InlineData("Response: After you attack or defend in Solid mass form, flip this card.")]
    [InlineData("Hero form only.")]
    [InlineData("Alter-ego form only.")]
    // A keyword is printed capitalised, on every one of the nine. Lower case is
    // prose that happens to end where a sentence does.
    [InlineData("energy form. Permanent.")]
    public void ProseNamingAFormDoesNotGrantIt(string printed)
    {
        Assert.Null(CardCatalog.FormOf(printed));
    }

    [Theory]
    [InlineData("Energy form. Permanent.", "energy")]
    [InlineData("Permanent. Mass form.", "mass")]
    [InlineData("Suit form. Permanent.\nInterrupt: When you attack.", "suit")]
    public void TheKeywordIsReadWhereverInTheKeywordLineItSits(string printed, string form)
    {
        // `21002` prints it first and `57046a` prints it second, so position is
        // not what identifies it.
        Assert.Equal(form, CardCatalog.FormOf(printed));
    }

    [Rule("rr:identity")]
    [Fact]
    public void AHeroFaceRegistersTheFieldsAHeroPrints()
    {
        // The bug this row exists to fix: before it, an identity in hero form
        // emitted an empty `fields` map, so a hero could take damage and the
        // digest would not show it.
        var world = Deal();
        var identity = world.Seats[0].IdentityCard;
        identity.TurnTo("01001a");

        var fields = StateFields.For(
            identity, Cards, players: 1, inPlay: true, hasHeldPools: true,
            hasFirstPlayerToken: true, world);

        // Spider-Man prints ATK 2, THW 1, DEF 3, HP 10, HS 5.
        Assert.Equal(2, fields["attack"]);
        Assert.Equal(1, fields["thwart"]);
        Assert.Equal(3, fields["defense"]);
        Assert.Equal(10, fields["health"]);
        Assert.Equal(5, fields["hand_size"]);

        // `recover` is an alter-ego field and a hero prints no REC, so a row
        // copied wholesale from the alter-ego's would put a zero on the wire
        // where there should be no key at all.
        Assert.DoesNotContain("recover", fields.Keys);
    }

    [Rule("rr:form-change-form.2")]
    [Fact]
    public void DamageOnAHeroSurvivesTheFlipToAlterEgo()
    {
        // "The character retains their sustained damage." Both faces are one
        // card, so this is really a check that `health` is computed from that
        // card's damage on either side rather than from whichever printed HP
        // happens to be showing -- and the two faces print different numbers
        // often enough for that to matter.
        var world = Deal();
        var identity = world.Seats[0].IdentityCard;
        identity.TurnTo("01001a");
        identity.TakeDamage(4);

        Assert.Equal(6, Health(world, identity));

        Forms.Change(world.Seats[0], Cards);

        // Peter Parker prints HP 10 as well, so the damage is what moved.
        Assert.Equal("01001b", identity.FaceId);
        Assert.Equal(6, Health(world, identity));
    }

    [Rule("rr:identity.1")]
    [Rule("rr:form-change-form.1")]
    [Fact]
    public void APlayerBeginsInAlterEgoFormAndMayChangeOnceInTheirTurn()
    {
        // "Each player begins the game in alter-ego form", and the recorded
        // opening board agrees -- `01001b` is the face in `HeroArea` at step 0.
        var world = Deal();
        var game = Game.Begin(world, Cards);
        var seat = world.Seats[0];

        Assert.Equal([Forms.AlterEgo], Forms.Of(world, seat, Cards));

        game.Resolve(Decision.Decline);   // keep the opening hand
        var change = game.Pending!.Affordances.Single(a => a.Verb == Game.ChangeForm);
        game.Resolve(Decision.Take(change.Id));

        Assert.Equal([Forms.Hero], Forms.Of(world, seat, Cards));
    }

    [Rule("rr:form-change-form.1")]
    [Rule("rr:player-turn.1")]
    [Fact]
    public void TheChangeIsOfferedOnceARoundAndTheTurnDoesNotEndOnIt()
    {
        // Two claims in one game because they are one rule. "Once each round"
        // is the limit, and "during their turn" is what says the turn carries
        // on -- changing form is something a player may do in a turn, not the
        // whole of it.
        var world = Deal();
        var game = Game.Begin(world, Cards);

        game.Resolve(Decision.Decline);
        var change = game.Pending!.Affordances.Single(a => a.Verb == Game.ChangeForm);
        game.Resolve(Decision.Take(change.Id));

        // Still the same turn, still being asked, and no longer offered.
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.Equal(1, world.Seats[0].FormChangedInRound);

        // The change is no longer on offer. The rest of the turn still is --
        // `rr:player-turn` limits only this one option to once each turn.
        Assert.DoesNotContain(game.Pending.Affordances, a => a.Verb == Game.ChangeForm);
    }

    [Rule("rr:activation.1")]
    [Fact]
    public void AVillainSchemesAgainstAnAlterEgoAndAttacksAHero()
    {
        // The reason form has to be readable at all, end to end. The same deal,
        // the same seed, the same villain; the only difference is which side of
        // one card is showing when the villain phase arrives.
        //
        // `rr:activation.1`: "if the player is in hero form, the enemy attacks
        // that player. If the player is in alter-ego form, the enemy schemes."
        var schemed = VillainPhaseAfter(changingForm: false);
        var attacked = VillainPhaseAfter(changingForm: true);

        // Rhino prints SCH 1 and the round-one boost card is worth nothing, so
        // his scheme adds 1 to the main scheme's own acceleration of 1.
        Assert.Equal(2, schemed.Threat);

        // Nothing to ask, so the phase ran to its end and the next question is
        // the following round's turn.
        Assert.Equal(Question.TurnOption, schemed.Asking);

        // Attacking instead, so the scheme stays where the main scheme's own
        // step put it, and the game stops to ask who defends.
        Assert.Equal(1, attacked.Threat);
        Assert.Equal(Question.Defender, attacked.Asking);
    }

    /// <summary>The villain phase, given a player who did or did not flip.</summary>
    private static (long Threat, Question? Asking) VillainPhaseAfter(bool changingForm)
    {
        var world = Deal();
        var game = Game.Begin(world, Cards);
        game.Resolve(Decision.Decline);   // keep the opening hand

        if (changingForm)
        {
            var change = game.Pending!.Affordances.Single(a => a.Verb == Game.ChangeForm);
            game.Resolve(Decision.Take(change.Id));
        }

        game.Resolve(Decision.Decline);   // end the turn
        EndPhase(game, world);            // step 1, into the villain phase

        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        return (scheme.Tokens.GetValueOrDefault("k_threat"), game.Pending?.Asking);
    }

    /// <summary>
    /// Answers <c>rr:end-of-player-phase.step.1</c>, discarding the excess.
    /// </summary>
    /// <remarks>
    /// <b>Not a decline, and that is the rule doing its job.</b> Peter Parker's
    /// hand size is 6 and Spider-Man's is 5, so a player who changes form and
    /// then ends the turn is holding one card too many — and step 1 says they
    /// "must discard down to their hand size". Which card goes is theirs to
    /// choose, so the engine refuses to choose for them.
    /// </remarks>
    private static void EndPhase(Game game, World world)
    {
        var seat = world.Seats[game.Pending!.Player];
        long limit = PhaseEnd.HandSize(world, seat, Cards);
        var excess = seat.Hand.Cards
            .Take(Math.Max(0, seat.Hand.Cards.Count - (int)limit))
            .Select(card => card.ObjectId)
            .ToArray();

        var affordance = game.Pending.Affordances.Single(a => a.Verb == Game.EndPhaseVerb);
        game.Resolve(Decision.Take(affordance.Id, excess, []));
    }

    private static long Health(World world, Card identity) =>
        StateFields.For(
            identity, Cards, players: 1, inPlay: true, hasHeldPools: true,
            hasFirstPlayerToken: true, world)["health"];

    private static World Deal() =>
        WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, Campaign, ["spider_man"]), Cards),
            [Setup.Hero("spider_man").Name],
            Seed);
}
