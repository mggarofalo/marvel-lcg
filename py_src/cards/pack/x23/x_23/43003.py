from . import *

# * Honey Badger: Gabby Kinney

def GetAbilities() -> Sequence['Ability']:

    def honey_badger(effect: 'Effect', message: 'Message.AfterUnitTookDamage') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)

    return [
        AbilityFactory.AfterUnitTookDamage(
            AbilityType.HeroResponse,
            "This",
            honey_badger
        ).SetTarget(name="X-23", canbe_ready=True),
    ]

