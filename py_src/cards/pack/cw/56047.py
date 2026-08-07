from . import *

# "I Can Do This All Day"

def GetAbilities() -> Sequence['Ability']:

    def i_can_do_this_all_day(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        assert isinstance(effect.targets[0], Unit2)
        message.DeclareDefender(effect.targets[0], effect)


    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.HeroInterrupt,
            Enemy,
            i_can_do_this_all_day
        ).SetPlay().SetLabel('defense')
        .SetTarget("YourHero"),
    ]

