from . import *

# Sentinel Mark V

def GetAbilities() -> Sequence['Ability']:

    def sentinel_mark_v_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if identity.GetInventoryDeck().FindCard(name="Targeted for Elimination"):
            this.DoAttackYou(player, effect)
        else:
            face = Search.EncounterCard(
                effect,
                include_discard_pile=True,
                name="Targeted for Elimination",
                card_type=Attachment,
            )
            if face:
                face.Reveal(None, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            sentinel_mark_v_revealed
        ),
    ]

