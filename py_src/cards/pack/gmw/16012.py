from . import *

# * Starhawk

def GetAbilities() -> Sequence['Ability']:

    def starhawk(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ReturnToHand([this], initiator, effect)


    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.Interrupt,
            "This",
            starhawk,
            took_excess_damage=False,
        ).SetTarget("This"),
    ]

