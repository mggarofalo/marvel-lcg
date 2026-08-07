from . import *

# * Clea

def GetAbilities() -> Sequence['Ability']:

    def clea(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        deck = this.GetOwnerPlayer().player_deck
        Faces.ShuffleAllTo([this], deck, effect)

    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.Interrupt,
            "This",
            clea
        ).SetTarget("This"),
    ]

