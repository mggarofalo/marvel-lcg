from . import *

# Cosmic Flight

def GetAbilities() -> Sequence['Ability']:

    def cosmic_flight(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.PreventDamage(3, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Captain Marvel"),
            trait="AERIAL",
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.HeroInterrupt,
            CardFinder(name="Captain Marvel"),
            cosmic_flight
        ).SetLabel('defense')
        .SetCostFunc(CostFunc.Discard("This"))
        .SetTarget("This")
    ]

