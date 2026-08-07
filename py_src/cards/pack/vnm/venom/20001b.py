from . import *

# * Flash Thompson

def GetAbilities() -> Sequence['Ability']:

    def flash_thompson(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        face = initiator.DiscardDeckUntil(
            effect,
            card_type=Upgrade,
            trait="WEAPON"
        )
        if face:
            initiator.GainCard(face, effect)


    return [
        AbilityFactory.ThisGainKeyword(
            control_additional_restricted=CardFinder(card_type=Upgrade),
        ),
        AbilityFactory.WhenCardSetup(
            "This",
            flash_thompson
        ).SetName("Armed and Ready"),
    ]

