from . import *

# Phantom Flip

def GetAbilities() -> Sequence['Ability']:

    def phantom_flip(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 5, effect)


    return [
        AfterGhostSpiderUsesBasicPower(
            AbilityType.HeroResponse,
            phantom_flip
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
        .LimitOncePerEvent(),
    ]

