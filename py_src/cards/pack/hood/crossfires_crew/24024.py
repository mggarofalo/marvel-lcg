from . import *

# * Controller

def GetAbilities() -> Sequence['Ability']:

    def controller(effect: 'Effect', message: 'Message.WhenFaceWouldDealDamage') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        if HasAttack.IsType(message.target):
            message.IncreaseDamage(message.target.attack, effect)


    return [
        AbilityFactory.WhenFaceWouldDealDamage(
            AbilityType.ForcedInterrupt,
            "This",
            controller,
            from_attack=True,
        )
    ]

