from . import *

# Electrostatic Armor

def GetAbilities() -> Sequence['Ability']:

    def electrostatic_armor(effect: 'Effect', message: 'Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players",
        ),
        AbilityFactory.AfterUnitDefendAgainstAttack(
            AbilityType.Response,
            "You",
            electrostatic_armor
        ).SetTarget("Attacker"),
    ]

