using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

// Commits one already-authored resource ability. Offer eligibility and resource
// generation remain in AbilityOfferQueries; this owner performs only payment
// and post-payment use recording through narrow capabilities.
internal sealed class AbilityResourceExecution
{
    private readonly AbilityProgram program;
    private readonly AbilityOfferQueries offers;

    internal AbilityResourceExecution(AbilityProgram program, AbilityOfferQueries offers)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(offers);
        this.program = program;
        this.offers = offers;
    }

    internal string UseResource(
        World world, int player, int card, List<GameEvent> events,
        IResourceCardAbilities resourceAbilities, ICardCounterPools pools)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(pools);

        var holder = world.Cards[card];
        var ability = offers.ResourceAbility(world, player, holder)
            ?? throw new RulesNotImplementedException(
                $"card {card} has no resource ability left to use this round");

        var payment = AbilityCostPayment.Prepare(
            world, holder, player, ability.Cost, [], [], program, resourceAbilities);
        payment.Commit(pools, Steps.TurnAction, events);
        AbilityUseRecording.Record(world, program, holder, ability);
        return AbilityOfferQueries.Generated(ability.Effect, world, player);
    }
}
