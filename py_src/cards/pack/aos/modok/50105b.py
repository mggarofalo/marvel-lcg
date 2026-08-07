from . import *

# Flying Inhuman

def GetAbilities() -> Sequence['Ability']:

    def flying_inhuman(effect: 'Effect', message: 'Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 1, effect)

    return [
        *AbilityFactory.ThisNotCountAllyLimit(),
        AbilityFactory.AfterUnitThwartEnd(
            AbilityType.Response,
            "This",
            flying_inhuman
        ).SetTarget(Scheme2, exclude="Targets"),
        FlyingInhumanLeavesPlay(),
    ]

