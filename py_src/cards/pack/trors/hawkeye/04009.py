from . import *

# Vibranium Arrow

def GetAbilities() -> Sequence['Ability']:

    def vibranium_arrow(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        # this.DealDamage(units, 6, effect)
        hero = initiator.GetHero()
        hero.DealDamage(effect.targets, 6, effect, property=AttackProperty(piercing=True))


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            vibranium_arrow,
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
        .SetCostFunc(CostFunc.Exhaust(
            card_type=Upgrade,
            name=HAWKEYE_BOW_NAME,
            from_where=["YouControlCards"])
        )
    ]

