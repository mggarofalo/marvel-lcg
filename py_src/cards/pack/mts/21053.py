from . import *

# * Major Victory: Vance Astro

def GetAbilities() -> Sequence['Ability']:

    def major_victory(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.Interrupt,
            "This",
            major_victory
        ).SetTarget(Friend, trait="GUARDIAN", canbe_ready=True),
    ]

