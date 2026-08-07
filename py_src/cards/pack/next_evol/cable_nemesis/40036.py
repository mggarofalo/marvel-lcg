from . import *

# Telekinetic Blast

def GetAbilities() -> Sequence['Ability']:

    def telekinetic_blast_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        damage = 2 + Worlds.VictoryDisplay(effect).FindCardSize(CardFinder(card_type=SchemeSide2))
        identity.TakeDamage(this, damage, effect)

    def telekinetic_blast_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        Faces.GiveStatus([identity], "Stunned", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            telekinetic_blast_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            telekinetic_blast_boost
        ),
    ]

