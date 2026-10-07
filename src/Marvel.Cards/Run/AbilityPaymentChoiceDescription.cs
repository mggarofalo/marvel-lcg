using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityStructuralFlowExecution;

namespace Marvel.Cards.Run;

/// <summary>Describes admitted resource-payment alternatives and their visible consequences.</summary>
internal static class AbilityPaymentChoiceDescription
{
    internal static Prompt Describe(
        AbilityStructuralContext context, AbilityEffect.PayOrEffect payment)
    {
        var world = context.Expressions.World;
        var sources = CardPayment.Generators(
            world, world.Facts, world.Seats[context.Player], context.ResourceAbilities);
        bool payable = Resources.Pays(string.Concat(sources.SelectMany(source => source.Generates)),
            payment.Resources.Length, payment.Resources);
        var offers = new List<Affordance>();
        if (payable)
        {
            offers.Add(new Affordance(0, AbilityStructuralExecution.ChooseVerb,
                context.Expressions.Source.ObjectId, World.Scenario, "spend",
                Costs: [new CostOption(context.Expressions.Source.ObjectId,
                    payment.Resources.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    [payment.Resources], Sources: sources)])
            {
                DisplayLabel = "Spend resources", CommitLabel = "Spend resources",
            });
        }
        if (!payment.ExhaustOnly)
        {
            offers.Add(Alternative(context, payment, "effect"));
        }
        else if (payment.Otherwise is AbilityEffect.CardAction exhaust
            && Every(exhaust.Selection, context).Any(card => card.Ready))
        {
            offers.Add(Alternative(context, payment, "exhaust"));
        }
        return new Prompt(context.Player, Question.Option, TimingPriority.Untimed,
            Steps.CardRevealed, $"{context.SourceFace}: spend or "
            + (payment.ExhaustOnly ? "exhaust" : "resolve"), false, offers)
        {
            ContextCardIds = [context.Expressions.Source.ObjectId],
            DisplayQuestion = $"{world.Facts.Title(context.SourceFace)}: choose an option",
        };
    }

    private static Affordance Alternative(AbilityStructuralContext context,
        AbilityEffect.PayOrEffect payment, string command)
    {
        var meaning = AbilityOptionDescription.From(context, payment.Otherwise);
        return new Affordance(1, AbilityStructuralExecution.ChooseVerb,
            context.Expressions.Source.ObjectId, World.Scenario, command)
        {
            DisplayLabel = meaning.Label ?? "Resolve the card effect",
            CommitLabel = meaning.Label ?? "Resolve the card effect",
            Description = meaning.Description,
        };
    }
}
