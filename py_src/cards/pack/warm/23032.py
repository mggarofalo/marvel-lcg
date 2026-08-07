from . import *

# As One!

def GetAbilities() -> Sequence['Ability']:

    def as_one(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        damage = 0
        for face in effect.cost_func.Get(CostFunc.Exhaust).return_exhausted_cards:
            if HasAttack.IsType(face):
                damage += face.attack
        this.DealDamage(effect.targets, damage, effect, property=AttackProperty(overkill=True))


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            as_one
        ).SetPlay().SetLabel('attack')
        .SetCostFunc(CostFunc.Exhaust(Select.Alliance(["AVENGER", "GUARDIAN"])))
        .SetTarget(Enemy),
    ]

