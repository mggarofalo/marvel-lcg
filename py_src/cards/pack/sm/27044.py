from . import *

# Field Agent

def GetAbilities() -> Sequence['Ability']:

    def field_agent(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        message.PreventDamage(1, effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.HeroInterrupt,
            CardFinder2("S.H.I.E.L.D", Ally),
            field_agent,
            is_consequential_damage=True,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'backup')),
    ]

