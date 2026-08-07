from . import *

# Psionic Inhuman

def GetAbilities() -> Sequence['Ability']:

    def psionic_inhuman(effect: 'Effect', message: 'Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.RemoveCountersOn(effect.targets, 1, 'lock', effect)


    return [
        *AbilityFactory.ThisNotCountAllyLimit(),
        AbilityFactory.AfterUnitThwartEnd(
            AbilityType.Response,
            "This",
            psionic_inhuman
        ).SetTarget(name="Holding Cell", has_counter='lock'),
        FlyingInhumanLeavesPlay(),
    ]

