from . import *

# Spider Claws

def GetAbilities() -> Sequence['Ability']:

    def spider_claws(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()

        value = GetTuckCardSameSetSize(initiator, message.attacked_targets, effect)
        message.GainATKForThisAttack(value, effect)
        message.GainPiercing(effect)


    return [
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.HeroInterrupt,
            CardFinder(name="Silk"),
            spider_claws,
            is_basic_attack=True
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("Trigger"),
    ]

