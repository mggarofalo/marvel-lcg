from . import *

# Web Binding

def GetAbilities() -> Sequence['Ability']:

    def web_binding(effect: 'Effect', message: 'Message.WhenEnemyWouldActivate') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.SetBeInstead(effect)

        minion = message.trigger
        if Minion.IsType(minion):
            this.DealDamage([minion], 4, effect)


    return [
        AbilityFactory.WhenEnemyWouldActivate(
            AbilityType.HeroInterrupt,
            None,
            web_binding
        ).SetPlay().SetLabel(),
    ]

