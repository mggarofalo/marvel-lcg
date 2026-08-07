from . import *

# Sisterly Bond

def GetAbilities() -> Sequence['Ability']:

    def sisterly_bond(effect: 'Effect', message: 'Message.WhenUnitWouldAttack|Message.WhenUnitWouldThwart') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        for face in effect.targets:
            message.AddMatchingPowerToThisPerformance(face, effect)


    return [
        AbilityFactory.WhenUnitWouldAttackOrThwart(
            AbilityType.HeroInterrupt,
            CardFinder(name="Honey Badger"),
            sisterly_bond
        ).SetPlay().SetLabel()
        .SetTarget(name="X-23"),
    ]

