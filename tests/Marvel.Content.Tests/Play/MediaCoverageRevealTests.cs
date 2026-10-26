using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MediaCoverageRevealTests
{
    [Theory]
    [InlineData(0, 0, false, false, 1)]
    [InlineData(1, 0, false, false, 2)]
    [InlineData(2, 0, false, false, 3)]
    [InlineData(1, 1, false, false, 1)]
    [InlineData(1, 0, true, false, 1)]
    [InlineData(1, 0, false, true, 1)]
    [Rule("rr:reveal.step.3")]
    public void OnlyLiveAuthoredAttachmentsOnTheRevealingIdentityAddFiniteCopies(
        int copies, int attachedSeat, bool departed, bool blank, int expected)
    {
        // 02049: "Resolve each 'When Revealed' ability that you reveal 1 additional time."
        // rr:reveal.step.3: "Resolve each When Revealed ability on that card".
        var f = Create();
        for (int index = 0; index < copies; index++)
        {
            var attachment = Attach(f, attachedSeat);
            if (departed) World.MoveToTop(attachment, f.World.AreaOf(DeckType.EncounterDiscardPile));
            if (blank) attachment.AssignProfile(new EffectiveCardProfile("blank", "Blank", CardKind.Attachment,
                [], ImmutableDictionary<string, long>.Empty));
        }
        var source = f.Reveal("01188");
        f.Finish();
        Assert.Equal(expected, f.World.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, f.World.Seats[1].IdentityCard.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.Empty(f.World.Agenda.Outstanding);
    }

    [Fact]
    public void CancelingTheRevelationCancelsEveryAdditionalResolution()
    {
        // ruling:49dc5a965dcd4028: Enhanced Spider-Sense cancels all When Revealed effects,
        // including additional effects caused by Media Coverage.
        var f = Create();
        Attach(f, 0);
        var source = f.Reveal("01188");
        f.World.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: "cancelWhenRevealed", Affects: source.ObjectId, Lasts: new Duration(Uses: 1)));
        f.Finish();
        Assert.Equal(0, f.World.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.Empty(f.World.Agenda.Outstanding);
    }

    [Fact]
    public void MerelyHavingThePhysicalAttachmentDoesNotImplementItsConstant()
    {
        var f = Create(authored: false);
        Attach(f, 0);
        f.Reveal("01188");
        f.Finish();
        Assert.Equal(1, f.World.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void NestedRevealsFinishTheirOwnCopiesBeforeTheParentRepeats()
    {
        var f = Create(effect: "{\"revealTop\":1}");
        Attach(f, 0);
        var deck = f.World.AreaOf(DeckType.EncounterDeck);
        f.World.CreateCard("01121", deck);
        var first = f.World.CreateCard("01190", deck);
        var second = f.World.CreateCard("01190", deck);
        var source = f.Reveal("01188");
        f.Finish();
        Assert.Equal(4, f.World.Seats[0].IdentityCard.Damage);
        Assert.All(new[] { source, first, second }, card =>
            Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type));
        Assert.Empty(f.World.Agenda.Outstanding);
    }

    [Fact]
    public void EachCopyWaitsForItsChoiceAndPreservesTheNonFirstRevealingPlayer()
    {
        var f = Create(effect: """
        {"chooseCard":{"from":{"query":"identities"},
          "effect":{"seq":[{"dealDamage":{"cards":"chosen","amount":1}},
            {"dealDamage":{"cards":"you","amount":1}}]}}}
        """);
        Attach(f, 1);
        var source = f.Reveal("01188", 1);
        int choices = 0;
        f.Finish(prompt =>
        {
            Assert.Equal(1, prompt.Player);
            Assert.Equal(DeckType.RevealingArea, source.Area.Type);
            Assert.Equal(choices, f.World.Seats[1].IdentityCard.Damage);
            choices++;
            return Decision.Take(f.World.Seats[0].IdentityCard.ObjectId);
        });
        Assert.Equal(2, choices);
        Assert.Equal(2, f.World.Seats[0].IdentityCard.Damage);
        Assert.Equal(2, f.World.Seats[1].IdentityCard.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.Empty(f.World.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false, 4)]
    [InlineData(true, 0)]
    [Rule("rr:surge.2")]
    public void FullEncounterProcedureRepeatsSurgeAndCancellationSuppressesEveryCopy(bool canceled, int damage)
    {
        // rr:surge.2: "complete the process of resolving the original card" before the additional card.
        var f = Create();
        Attach(f, 1);
        var deck = f.World.AreaOf(DeckType.EncounterDeck);
        var first = f.World.CreateCard("01190", deck);
        var second = f.World.CreateCard("01190", deck);
        var source = f.Reveal("01121", 1); // Weapons Runner has printed Surge and no When Revealed text.
        f.World.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCards), 1, 4, Plan: true));
        if (canceled)
            f.World.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                Kind: "cancelWhenRevealed", Affects: source.ObjectId, Lasts: new Duration(Uses: 1)));
        f.Finish();
        Assert.Equal(damage, f.World.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, f.World.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, source.Area.Type);
        Assert.All(new[] { first, second }, card => Assert.Equal(canceled
            ? DeckType.EncounterDeck : DeckType.EncounterDiscardPile, card.Area.Type));
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Empty(f.World.Agenda.Outstanding);
    }

    [Fact]
    public void VillainStageEntryDoesNotRepeatEvenWithFirstPlayerContext()
    {
        // ruling:c516c82788ea8d77: "A villain stage is not revealed by a specific player".
        // Stage promotion uses ExecuteReveal with first-player context for its printed "you".
        var f = Create();
        Attach(f, 0);
        f.Runner.ExecuteReveal(f.World, f.Villain, f.World.FirstPlayer);
        f.Finish();
        Assert.Equal(1, f.World.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, f.World.Seats[1].IdentityCard.Damage);
    }

    [Fact]
    public void ReplayingSerializedChoiceDecisionsReconstructsTheRemainingCopy()
    {
        var original = Create(effect: ChoiceEffect);
        var replay = Create(effect: ChoiceEffect);
        Attach(original, 1);
        Attach(replay, 1);
        original.Reveal("01188", 1);
        replay.Reveal("01188", 1);
        for (int choice = 0; choice < 2; choice++)
        {
            var originalPrompt = Sequence.Work(original.World, original.World.Facts, original.Runner, []);
            var replayPrompt = Sequence.Work(replay.World, replay.World.Facts, replay.Runner, []);
            Assert.NotNull(originalPrompt);
            Assert.NotNull(replayPrompt);
            Assert.Equal(1, replayPrompt.Player);
            Assert.Equal(original.World.Digest().Canonical(), replay.World.Digest().Canonical());
            var decision = Decision.Take(original.World.Seats[0].IdentityCard.ObjectId);
            var restored = System.Text.Json.JsonSerializer.Deserialize<Decision>(
                System.Text.Json.JsonSerializer.Serialize(decision))!;
            Sequence.Answer(original.World, original.World.Facts, original.Runner, originalPrompt, decision, []);
            Sequence.Answer(replay.World, replay.World.Facts, replay.Runner, replayPrompt, restored, []);
        }
        original.Finish();
        replay.Finish();
        Assert.Equal(2, replay.World.Seats[0].IdentityCard.Damage);
        Assert.Equal(original.World.Digest().Canonical(), replay.World.Digest().Canonical());
        Assert.Equal(original.World.Random.Generator.WordsConsumed, replay.World.Random.Generator.WordsConsumed);
        Assert.Empty(replay.World.Agenda.Outstanding);
    }

    private const string ChoiceEffect = """
        {"chooseCard":{"from":{"query":"identities"},
          "effect":{"dealDamage":{"cards":"chosen","amount":1}}}}
        """;

    [Fact]
    public void SelectedAbilityFinishesItsCopiesBeforeAnotherAbilityIsChosen()
    {
        // rr:simultaneous-resolution orders effects sharing a bold trigger.
        // Engine interpretation under review: the selected ability finishes its additional
        // resolutions before selecting another ability on the same revealed card.
        var f = Create(effect: ChoiceEffect, extra: """
        ,{"trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
          "effect":{"chooseCard":{"from":{"query":"identities"},
            "effect":{"dealDamage":{"cards":"chosen","amount":2}}}}}
        """);
        Attach(f, 1);
        f.Reveal("01188", 1);
        var before = new List<long>();
        int orderings = 0;
        f.Finish(prompt =>
        {
            if (prompt.Asking == Marvel.Rules.Prompts.Question.Order)
            {
                Assert.Equal(0, prompt.Player);
                orderings++;
                return Decision.Take(prompt.Affordances[1].Id);
            }
            before.Add(f.World.Seats[0].IdentityCard.Damage);
            return Decision.Take(f.World.Seats[0].IdentityCard.ObjectId);
        });
        Assert.Equal(1, orderings);
        Assert.Equal(new long[] { 0, 2, 4, 5 }, before);
        Assert.Equal(6, f.World.Seats[0].IdentityCard.Damage);
    }

    private static Card Attach(RedSkullFixture f, int seat)
    {
        var identity = f.World.Seats[seat].IdentityCard;
        return f.World.CreateCard("02049",
            f.World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(seat), identity.ObjectId));
    }

    private static RedSkullFixture Create(bool authored = true, string? effect = null, string extra = "")
    {
        var f = RedSkullFixture.Create(players: 2);
        // Local interpretations exercise the general repeat mechanism without public admission.
        // 01188's physical identity is preserved; its synthetic one-damage text distinguishes copies.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
        {"cards":[
          {"card":"02049","abilities":MEDIA},
          {"card":"01121","abilities":[]},
          {"card":"STAGE","abilities":[{
            "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
            "effect":{"dealDamage":{"cards":"you","amount":1}}
          }]},
          {"card":"01188","abilities":[{
            "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
            "effect":EFFECT
          }EXTRA]},
          {"card":"01190","abilities":[{
            "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
            "effect":{"dealDamage":{"cards":"you","amount":1}}
          }]}
        ]}
        """.Replace("STAGE", f.Villain.FaceId, StringComparison.Ordinal)
        .Replace("EFFECT", effect ?? "{\"dealDamage\":{\"cards\":\"you\",\"amount\":1}}", StringComparison.Ordinal)
        .Replace("EXTRA", extra, StringComparison.Ordinal)
        .Replace("MEDIA", authored ? """
        [{"trigger":{"timing":"Constant","subject":"this"},
          "effect":{"grant":{"card":"attachedTo","keyword":"additionalWhenRevealed","amount":1}}}]
        """ : "[]", StringComparison.Ordinal)));
        f.World.Abilities = runner;
        return new(f.World, runner);
    }
}
