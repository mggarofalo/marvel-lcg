from . import *

# * Illyana Rasputin

def GetAbilities() -> Sequence['Ability']:

    def illyana_rasputin(effect: 'Effect', message: 'Message.WhenUnitWouldChangeForm') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()

        Faces.MoveAllToDeck(effect.targets, initiator.player_deck, "Top", effect)

    return [
        AbilityFactory.WhenUnitWouldChangeForm(
            AbilityType.Interrupt,
            "You",
            illyana_rasputin,
            to_form=Hero
        ).SetTarget(trait="SPELL", from_where=["YourDiscardPile"])
        .LimitOncePerPhase(),
    ]

