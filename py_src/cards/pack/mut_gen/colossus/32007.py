from . import *

# Made of Rage

def GetAbilities() -> Sequence['Ability']:

    def made_of_rage(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.GainATKForThisAttack(+6, effect)
        message.GainOverKill(effect)

    return [
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.HeroInterrupt,
            "You",
            made_of_rage,
            is_basic_attack=True,
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Discard("YourHeroTough")),
    ]

