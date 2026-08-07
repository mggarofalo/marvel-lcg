from . import *

# Sovereign Sorceress

def GetAbilities() -> Sequence['Ability']:

    def sovereign_sorceress_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        player = message.GetToPlayer()

        face = Worlds.FindCardOnField(
            effect,
            name="Crown of the Enchantress",
            card_type=Attachment
        )
        if face:
            identities = Worlds.GetPlayersIdentities(effect)
            Faces.GiveStatus(identities, "Stunned", effect)
        else:
            face = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=True,
                name="Crown of the Enchantress",
            )
            if face:
                face.Reveal(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            sovereign_sorceress_revealed
        ),
    ]

