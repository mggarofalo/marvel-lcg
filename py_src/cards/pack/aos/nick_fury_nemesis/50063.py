from . import *

# Cold Storage

def GetAbilities() -> Sequence['Ability']:

    def cold_storage_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        face = Search.EncounterCard(
            effect,
            player,
            include_discard_pile=True,
            trait="LEVIATHAN",
            card_type=Minion
        )
        if face:
            face.PutIntoPlay(player, effect)
        else:
            identity = player.GetIdentity()
            identity.TakeDamage(this, 2, effect)

    def cold_storage_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        identity.TakeDamage(this, 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            cold_storage_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            cold_storage_boost
        ),
    ]

