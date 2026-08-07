from . import *

# Telekinetic Blast

def GetAbilities() -> Sequence['Ability']:

    def telekinetic_blast(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        damage = 6
        damage += Worlds.VictoryDisplay(effect).FindCardSize(CardFinder(card_type=SchemeSide2))
        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            telekinetic_blast
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

