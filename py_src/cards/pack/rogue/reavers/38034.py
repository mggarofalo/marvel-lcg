from . import *

# The Reavers

def GetAbilities() -> Sequence['Ability']:

    def the_reavers(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        face = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Minion,
            trait="REAVER",
        )
        if face:
            face.Reveal(player, effect)

    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            the_reavers,
            has_defeating_player=True
        ),
    ]

