from . import *

# Pitchback

def GetAbilities() -> Sequence['Ability']:

    def pitchback(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 4, effect)


    return [
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.HeroResponse,
            "YourHero",
            pitchback
        ).SetPlay(only_if_your_identity_has_trait="AERIAL").SetLabel('attack')
        .SetTarget(Enemy),
    ]

