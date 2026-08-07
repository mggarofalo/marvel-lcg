from . import *

# Anticipated Attack

def GetAbilities() -> Sequence['Ability']:

    def anticipated_attack(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Tough", effect)


    return [
        AbilityFactory.WhenUnitInitiatesAttackAgainst(
            AbilityType.HeroInterrupt,
            Enemy,
            "AnyPlayer",
            anticipated_attack
        ).SetPlay(only_if_there_is_side_scheme_in_victory=True).SetLabel('defense')
        .SetTarget("YourHero", canbe_tough=True),
    ]

