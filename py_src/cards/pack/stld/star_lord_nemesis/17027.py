from . import *

# Spartoi Cunning

def GetAbilities() -> Sequence['Ability']:

    def spartoi_cunning_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardRandomHandCards(1, effect)
        player.GetIdentity().TakeDamage(this, 1, effect)
        scheme = Worlds.FindMainScheme(this)
        if scheme:
            this.PlaceThreatOnSchemes([scheme], 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            spartoi_cunning_revealed
        ),
    ]

