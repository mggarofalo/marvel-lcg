from . import *

# * Jubilee's Sunglasses

def GetAbilities() -> Sequence['Ability']:

    def jubilees_sunglasses(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        damage = message.paid_resources.GetResourceIconTypes()
        this.DealDamage(effect.targets, damage, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Jubilee"),
            attack=1,
        ),
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.HeroResponse,
            "You",
            CardFinder2("ATTACK", Event),
            jubilees_sunglasses
        ).SetLabel('attack')
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Enemy),
    ]

