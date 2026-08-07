from . import *

# Host Spider

def GetAbilities() -> Sequence['Ability']:

    def host_spider(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            host_spider
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(name="SP//dr Suit", canbe_ready=True),
    ]

