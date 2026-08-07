from . import *

# Homeland Intervention

def GetAbilities() -> Sequence['Ability']:

    def homeland_intervention(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        value = 2 * len(effect.cost_func.Get(CostFunc.Exhaust).return_exhausted_cards)
        this.RemoveThreatFromSchemes(effect.targets, value, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            homeland_intervention
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Exhaust(
            trait="S.H.I.E.L.D",
            range=(1, 3),
            from_where=["YouControlCards"]))
        .SetTarget(Scheme2),
    ]

