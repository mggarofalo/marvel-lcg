from . import *

# Assess the Situation

def GetAbilities() -> Sequence['Ability']:

    def assess_the_situation(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        target = effect.targets[0].CastTo(Identity)
        target.GainUntilPhaseEnd(effect, hand_size=1)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            assess_the_situation,
        ).SetPlay()
        .SetTarget("YourIdentity"),
    ]

