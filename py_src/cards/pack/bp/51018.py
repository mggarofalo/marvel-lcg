from . import *

# * The Raft

def GetAbilities() -> Sequence['Ability']:

    def the_raft(effect: 'Effect', message: 'Message.AfterCardLeavePlay') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        value = effect.cost_func.Get(CostFunc.TuckCardHere).tucked_cards[0].CastTo(Minion).printed_scheme
        this.RemoveThreatFromSchemes(effect.targets, value, effect)

        if this.GetPlacedCardArea().GetSize() >= 4:
            random_minion = Rand.RandomChoice(this.GetPlacedCardArea().GetAll(), effect)

            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        targets[0].GetControlByPlayer().DealEncounterCard(random_minion, effect)
                ).SetTarget("Players")
            )

    return [
        AbilityFactory.AfterCardLeavePlay(
            AbilityType.Response,
            Minion,
            the_raft
        ).SetCostFunc(CostFunc.TuckCardHere("Trigger", must_from_encounter_discard_pile=True))
        .SetTarget(Scheme2),
    ]

