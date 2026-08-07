from . import *

# Electromagnetic Backlash

def GetAbilities() -> Sequence['Ability']:

    def electromagnetic_backlash(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)

        def action(player: 'Player'):
            identity = player.GetIdentity()
            faces = player.DiscardDeckTopCards(5, effect)
            damage = FacesCounter.GetPrintedResourcesCount(faces, "Y", player.player_deck)
            identity.TakeDamage(this, damage, effect)

        Players.ForEachPlayer(effect, action)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            electromagnetic_backlash
        )
    ]

