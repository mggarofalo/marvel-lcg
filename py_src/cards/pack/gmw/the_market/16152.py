from . import *

# Contingency Plan

def GetAbilities() -> Sequence['Ability']:

    def contingency_plan(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = initiator.DiscardDeckTopCards(4, effect)
        value = FacesCounter.GetPrintedResourcesTypes(faces)
        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    this.DealDamageTotal(targets, value, effect)
            ).SetTarget(Enemy, range=(1, value), repeat_rules="Health")
        )
        initiator.DrawUp(1, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            contingency_plan
        ).SetPlay().SetLabel('attack')
        .SetHasNoTargetEffect(),
    ]

