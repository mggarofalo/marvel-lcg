from . import *

# * Groot

def GetAbilities() -> Sequence['Ability']:

    def groot(effect: 'Effect', message: 'Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.HealthUnits(effect.targets, 2, effect)


    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_your_identity_has_trait="GUARDIAN"),
        AbilityFactory.AfterUnitDefendAgainstAttack(
            AbilityType.Response,
            "This",
            groot
        ).SetTarget("This", canbe_heal=True)
    ]

