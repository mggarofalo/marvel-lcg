from . import *

# Vine Spikes

def GetAbilities() -> Sequence['Ability']:

    def vine_spikes(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.GainATKForThisAttack(+2, effect)


    return [
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.HeroInterrupt,
            CardFinder(name="Groot"),
            vine_spikes,
            is_basic_attack=True,
        ).SetCostFunc(CostFunc.Counter(Select.From("Trigger"), 1, 'growth'))
        .SetCostFunc(CostFunc.Exhaust("This")),
    ]

