from . import *

# * Slab

def GetAbilities() -> Sequence['Ability']:

    def slab(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'growth', effect)

        this.GainForThisActive(
            effect,
            message,
            attack=this.GetCounters('growth')
        )


    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.ForcedInterrupt,
            "This",
            slab
        ),
    ]

