from . import *

# * Enchantress

def GetAbilities() -> Sequence['Ability']:

    def enchantress_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        face = Search.EncounterCard(
            effect,
            player,
            include_discard_pile=True,
            include_set_aside=True,
            name="Seduced",
            card_type=Attachment
        )
        if face:
            face.AttachTo2(identity, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            enchantress_revealed
        ),
    ]

