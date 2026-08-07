from . import *

# Da Bomb

def GetAbilities() -> Sequence['Ability']:

    def da_bomb(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 10, effect)
        units = Worlds.GetOnFieldEnemies(effect)
        damage = Worlds.GetIconsNum(effect, ["Crisis", "Acceleration", "Amplify", "Hazard"])

        this.DealDamage(units, damage, effect)

        for unit in Worlds.GetOnFieldHeroes(effect):
            unit.TakeDamage(this, damage, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            da_bomb
        ).SetPlay().SetLabel()
        .SetTarget(Villain),
    ]

