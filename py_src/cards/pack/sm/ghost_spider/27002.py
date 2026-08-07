from . import *

# Ghost Kick

def GetAbilities() -> Sequence['Ability']:

    def ghost_kick(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 6, effect)


    return [
        AfterGhostSpiderUsesBasicPower(
            AbilityType.HeroResponse,
            ghost_kick
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
        .LimitOncePerEvent(),
    ]

