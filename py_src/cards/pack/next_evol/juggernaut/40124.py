from . import *

# Building Momentum

def GetAbilities() -> Sequence['Ability']:

    def building_momentum(effect: 'Effect', message: 'Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        this.RemoveThreatFromSchemes([this], 1, effect)


    return [
        AbilityFactory.AfterUnitDefendAgainstAttack(
            AbilityType.HeroResponse,
            "You",
            building_momentum,
            attacker=CardFinder(name="Juggernaut")
        ),
    ]

