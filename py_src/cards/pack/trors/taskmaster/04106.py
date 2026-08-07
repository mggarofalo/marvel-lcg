from . import *

# Hunted by Hydra

def GetAbilities() -> Sequence['Ability']:

    def hunted_by_hydra_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action(player: 'Player'):
            if player.IsInHeroForm():
                player.GetHero().TakeDamage(this, 1, effect)
                player.DiscardRandomHandCards(1, effect)

        Players.ForEachPlayer(effect, action)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            hunted_by_hydra_revealed
        ),
    ]

