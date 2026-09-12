using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;
internal static class ActionAbilityOrderedPaymentFixtures
{
    internal static (AbilityProgram Program, AbilityRunner Runner) PaymentRunner(string card, string effect, string cost)
    {
        var book = AbilityCatalog.Parse($$"""
            {"cards":[{"card":"{{card}}","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{{cost}},
              "effect":{{effect}}
            }]}]}
            """);
        var program = AbilityLowering.Book(book);
        return (program, new AbilityRunner(program));
    }

    internal static (World World, Card Source) OrderedPaymentBoard(AbilityRunner runner)
    {
        var(world, source) = FixedCountBoard(runner);
        // Keep discard observations separate from the empty-player-deck procedure.
        world.CreateCard("01088", world.Seats[0].Deck);
        return (world, source);
    }

    internal sealed class PaymentResourceProbe(Card generator, string resources, Action commit) : NoCardAbilities
    {
        internal int Uses { get; private set; }

        public override IReadOnlyList<ResourceSource> ResourceAbilities(World world, int player) => [new(generator.ObjectId, resources)];
        public override string UseResource(World world, int player, int card, List<GameEvent> events)
        {
            Assert.Equal(generator.ObjectId, card);
            Uses++;
            commit();
            return resources;
        }
    }

    internal sealed class UnrelatedPaymentAbilities : NoCardAbilities
    {
        public override bool CanTakeDamage(World world, Card target, Card source) => throw new InvalidOperationException("the unrelated World damage port was used");
        public override bool CanRemoveThreat(World world, Card scheme, int ignoredSource = -1) => throw new InvalidOperationException("the unrelated World threat port was used");
        public override IReadOnlyList<ResourceSource> ResourceAbilities(World world, int player) => throw new InvalidOperationException("the unrelated World resource port was used");
        public override string ResourcesGeneratedBy(World world, Card source, Card? payingFor) => throw new InvalidOperationException("the unrelated World resource port was used");
        public override string UseResource(World world, int player, int card, List<GameEvent> events) => throw new InvalidOperationException("the unrelated World resource port was used");
    }
}
