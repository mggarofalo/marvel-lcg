from . import *

# Under Siege

def GetAbilities() -> Sequence['Ability']:

    def under_siege(effect: 'Effect', message: 'Message.WhenMainSchemeStageWouldBeCompleted') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        message.SetBeInstead(effect)
        this.RemoveThreatFromSchemes([this], "All", effect)

        DealDamageToAvengersTower("6*", effect)


    return [
        AbilityFactory.WhenMainSchemeStageWouldBeCompleted(
            AbilityType.WhenCompleted,
            "This",
            under_siege,
        ),
    ]

