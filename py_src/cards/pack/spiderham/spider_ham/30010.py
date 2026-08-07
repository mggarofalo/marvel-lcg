from . import *

# Huge Wooden Hammer

def GetAbilities() -> Sequence['Ability']:

    def huge_wooden_hammer(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.GainATKForThisAttack(+2, effect)
        message.GainOverKill(effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Spider-Ham"),
            attack=1,
        ),
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.HeroInterrupt,
            CardFinder(name="Spider-Ham"),
            huge_wooden_hammer,
            is_basic_attack=True,
        ).SetCostFunc(CostFunc.Counter(Select.From("YourIdentity"), 1, 'toon'))
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("This"),
    ]

