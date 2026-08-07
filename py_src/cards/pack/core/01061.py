from . import *

# Great Responsibility

def GetAbilities() -> Sequence['Ability']:

    def great_responsibility(effect: 'Effect', message: 'Message.WhenSchemeWouldPlaceThreat') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.SetBeInstead(effect)
        value = message.value
        effect.targets[0].CastTo(Hero).TakeDamage(this, value, effect)


    return [
        AbilityFactory.WhenThreatWouldBePlacedOn(
            AbilityType.HeroInterrupt,
            None,
            great_responsibility
        ).SetPlay()
        .SetTarget("YourIdentity")
    ]

