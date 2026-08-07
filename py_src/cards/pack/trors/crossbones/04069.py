from . import *

# Raid the Armory

def GetAbilities() -> Sequence['Ability']:

    def raid_the_armory_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        face = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Attachment,
            trait="WEAPON"
        )
        if face:
            face.Reveal(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            raid_the_armory_revealed
        ),
    ]

