from . import *

# Entangling Vines

def GetAbilities() -> Sequence['Ability']:

    def entangling_vines(effect: 'Effect', message: 'Message.WhenUnitWouldThwart') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.GainTHWForThisThwart(+2, effect)


    return [
        AbilityFactory.WhenUnitMakeThwart(
            AbilityType.HeroInterrupt,
            CardFinder(name="Groot"),
            None,
            entangling_vines,
            is_basic_thwart=True,
        ).SetCostFunc(CostFunc.Counter(Select.From("Trigger"), 1, 'growth'))
        .SetCostFunc(CostFunc.Exhaust("This")),
    ]

