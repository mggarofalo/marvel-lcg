from . import *

# Wiggle Room

def GetAbilities() -> Sequence['Ability']:

    def wiggle_room(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        message.PreventDamage(3, effect)
        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.HeroInterrupt,
            "You",
            wiggle_room
        ).SetPlay().SetLabel('defense'),
    ]

