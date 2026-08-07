from . import *

# Parry

def GetAbilities() -> Sequence['Ability']:

    def parry(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        value = initiator.GetHero().attack * 2
        message.PreventDamage(value, effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.HeroInterrupt,
            "You",
            parry
        ).SetPlay().SetLabel('defense')
    ]

