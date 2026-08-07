from . import *

# Knife Leap

def GetAbilities() -> Sequence['Ability']:

    def knife_leap(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.GainATKForThisAttack(5, effect)
        message.GainOverKill(effect)
        message.GainPiercing(effect)


    return [
        AbilityFactory.ReduceCostToPlayThis(
            lambda effect:
                effect.GetInitiator().GetHero().GetCounters('vengeance'),
        ),
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.HeroInterrupt,
            "You",
            knife_leap,
            is_basic_attack=True,
        ).SetPlay().SetLabel(),
    ]

