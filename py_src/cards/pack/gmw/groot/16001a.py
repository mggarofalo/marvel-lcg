from . import *

# * Groot

def GetAbilities() -> Sequence['Ability']:

    def groot(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        value = Faces.RemoveCountersOn([this], message.will_take_damage, 'growth', effect)
        if value:
            message.PreventDamage(value, effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            "This",
            groot
        ).SetName("Flora Colossus"),
    ]

