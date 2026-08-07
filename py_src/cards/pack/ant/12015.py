from . import *

# Call for Aid

def GetAbilities() -> Sequence['Ability']:

    def call_for_aid(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        face = initiator.DiscardDeckUntil(
            effect,
            card_type=Ally,
            trait="AVENGER"
        )
        if face:
            initiator.GainCard(face, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            call_for_aid
        ).SetPlay()
    ]

