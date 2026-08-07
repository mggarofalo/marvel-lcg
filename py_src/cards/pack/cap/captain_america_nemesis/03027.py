from . import *

# Hit Squad

def GetAbilities() -> Sequence['Ability']:

    def hit_squad(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)

        def action(player: 'Player'):
            faces = Worlds.DiscardEncounterCards(1, effect)
            damage = FacesCounter.CountTotalBoostIcons(faces)
            player.GetIdentity().TakeDamage(this, damage, effect)

        Players.ForEachPlayer(effect, action)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            hit_squad
        )
    ]
