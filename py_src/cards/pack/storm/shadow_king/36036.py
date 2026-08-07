from . import *

# * The Shadow King

def GetAbilities() -> Sequence['Ability']:

    def the_shadow_king_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        face = Search.EncounterCard(
            effect,
            player,
            include_discard_pile=True,
            name="Possessed",
            card_type=Attachment
        )
        if face:
            face.Reveal(player, effect)


    def the_shadow_king(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> bool:
        this = effect.this.CastTo(Minion)
        Unused(this)

        return Worlds.FindCardOnField(
            effect,
            trait="CONTROLLED",
            card_type=Minion
        ) != None


    return [
        AbilityFactory.ThisCannotTakeDamageWhile(
            conditions=[the_shadow_king]
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            the_shadow_king_revealed
        ),
    ]

