from . import *

# * Shocker

def GetAbilities() -> Sequence['Ability']:

    def shocker(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)

        heroes = Worlds.GetOnFieldHeroes(effect)
        this.DealDamage(heroes, 1, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            shocker
        )
    ]
