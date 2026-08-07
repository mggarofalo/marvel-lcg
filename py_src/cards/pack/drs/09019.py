from . import *

# * The Night Nurse

def GetAbilities() -> Sequence['Ability']:

    def the_night_nurse(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        for target in effect.targets:
            this.HealthUnits([target], 1, effect)

            initiator = effect.GetInitiator()
            status = target.components.status.GetDeck().Get()
            status_face = initiator.AskChooseFace(status, effect)
            if status_face:
                Faces.DiscardAll([status_face], effect)

    def select_can_health_or_has_state_heroes(effect: 'Effect')-> Sequence['Hero']:
        return [x for x in Worlds.GetOnFieldHeroes(effect) if x.CanHeath() or x.HasStatus('Any')]

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            the_night_nurse
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'medical'))
        .SetTarget(select_can_health_or_has_state_heroes)
    ]

