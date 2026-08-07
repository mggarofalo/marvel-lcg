from . import *

# Hack 'n' Slash

def GetAbilities() -> Sequence['Ability']:

    def hack_n_slash(effect: 'Effect', message: 'Message.WhenCardRevealed|Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        faces = player.DiscardRandomHandCards(1, effect)
        damage = FacesCounter.GetPrintedResourcesIcon(faces, player.player_deck)
        identity.TakeDamage(this, damage, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            hack_n_slash
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            hack_n_slash
        ),
    ]

