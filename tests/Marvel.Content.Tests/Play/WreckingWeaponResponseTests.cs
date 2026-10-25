using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingWeaponResponseTests
{
    [Theory]
    [InlineData("07006", 0, false, false)]
    [InlineData("07006", 0, true, false)]
    [InlineData("07006", 0, false, true)]
    [InlineData("07020", 1, false, false)]
    [InlineData("07020", 1, true, false)]
    [InlineData("07020", 1, false, true)]
    [Rule("rr:stun-stunned.7")]
    [Rule("rr:first-player.1")]
    public void OnlyTheAttachedVillainsCompletedAttackPlacesThePrintedThreat(
        string face, int host, bool cancelled, bool foreign)
    {
        // A stunned enemy "is not considered to have attacked". Magic Crowbar's
        // least-threat tie is an encounter target decision for the first player.
        var (world, groups, source) = WreckingEncounterCardFixture.Create(face, 2, players: 2);
        world.CreateCard("01088", world.Seats[1].Deck);
        world.FirstPlayer = 1;
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        long baseAttack = StateFields.Modified(world, groups[host].Villain, "attack", world.Facts, 2);
        Reveal.Resolve(world, world.Facts, source, 0, []);
        Assert.Equal(groups[host].Villain.ObjectId, source.Area.Host);
        Assert.Equal(baseAttack + 1,
            StateFields.Modified(world, groups[host].Villain, "attack", world.Facts, 2));
        long[] initial = [3, 4, 1, 1];
        for (int index = 0; index < groups.Length; index++)
            groups[index].Scheme.PlaceTokens("k_threat", initial[index]);
        var attacker = groups[foreign ? 3 : host].Villain;
        if (cancelled) Statuses.Give(world, attacker, Statuses.Stunned);
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Tough);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 1,
            Subject: attacker.ObjectId, Seat: 0));
        int choices = Finish(world, groups[3].Scheme);
        bool responds = !cancelled && !foreign;
        Assert.Equal(face == "07006" && responds ? 1 : 0, choices);
        AssertThreat(world, groups, face, responds, initial);
        Assert.Equal(groups[host].Villain.ObjectId, source.Area.Host);
        Assert.Same(groups[1], world.EncounterTopology!.Active);
    }

    private static void AssertThreat(World world, EncounterDeckGroup[] groups,
        string face, bool responds, long[] initial)
    {
        Assert.Equal(face == "07020" && responds ? 1 : 0,
            world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
        for (int index = 0; index < groups.Length; index++)
            Assert.Equal(initial[index] + (face == "07006" && responds && index == 3 ? 1 : 0),
                groups[index].Scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    private static int Finish(World world, Card chosen)
    {
        int choices = 0;
        for (int decisions = 0; ; decisions++)
        {
            var prompt = Sequence.Work(world, world.Facts, world.Abilities, []);
            if (prompt is null) return choices;
            Assert.True(decisions < 20, prompt.Label);
            var answer = Decision.Decline;
            if (prompt.Affordances.Any(offer => offer.Id == chosen.ObjectId))
            {
                Assert.Equal(1, prompt.Player);
                Assert.Equal(2, prompt.Affordances.Count);
                answer = Decision.Take(chosen.ObjectId);
                choices++;
            }
            else if (!prompt.Cancellable)
            {
                var offer = prompt.Affordances.First(item => item.IsLegal);
                answer = new(offer.Id, offer.Targets?.Legal.Take(offer.Targets.Min).ToArray() ?? []);
            }
            Sequence.Answer(world, world.Facts, world.Abilities, prompt, answer, []);
        }
    }
}
