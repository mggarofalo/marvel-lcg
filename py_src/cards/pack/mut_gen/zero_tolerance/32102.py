from . import *

# Sentinel Mark III

def GetAbilities() -> Sequence['Ability']:

    def sentinel_mark_iii_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        face = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            name="Energy Barrier",
            card_type=Attachment
        )
        if face:
            face.AttachTo2(this, effect)

    def sentinel_mark_iii_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if identity.IsStunned():
            identity.TakeDamage(this, 2, effect)
        else:
            Faces.GiveStatus([identity], "Stunned", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            sentinel_mark_iii_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            sentinel_mark_iii_boost
        ),
    ]

