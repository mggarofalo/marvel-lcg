from . import *

# Multitasking

def GetAbilities() -> Sequence['Ability']:

    def multitasking(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        this.RemoveThreatFromSchemes(effect.targets, 2, effect)
        if effect.GetPaidResources().GetColor("B"):
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.RemoveThreatFromSchemes(targets, 2, effect)
                ).SetLabel('thwart')
                .SetTarget(Scheme2, exclude=effect.targets),
            )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            multitasking
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

