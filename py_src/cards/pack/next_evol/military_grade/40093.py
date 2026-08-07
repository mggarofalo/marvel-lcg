from . import *

# The Senator's Support

def GetAbilities() -> Sequence['Ability']:

    def the_senators_support(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = Worlds.GetFirstPlayer(effect)

        face = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Attachment,
        )

        if face:
            face.Reveal(player, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            the_senators_support,
        ),
    ]

