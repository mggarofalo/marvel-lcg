from . import *

# Crisis Interdiction

def GetAbilities() -> Sequence['Ability']:

    def crisis_interdiction(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 2, effect)
        initiator = effect.GetInitiator()
        if initiator.GetIdentity().HasTrait('AERIAL'):

            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.RemoveThreatFromSchemes(targets, 2, effect)
                ).SetLabel('thwart')
                .SetTarget(Scheme2, exclude=effect.targets)
            )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            crisis_interdiction
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
    ]

