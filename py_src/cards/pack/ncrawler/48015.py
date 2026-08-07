from . import *

# Under Control

def GetAbilities() -> Sequence['Ability']:

    def under_control(effect: 'Effect', message: 'Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        unit = this.GetBindFace()
        this.DealDamage([unit], 4, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(Minion),
        AbilityFactory.AfterUnitDefendAgainstAttack(
            AbilityType.Response,
            Hero,
            under_control,
            attacker="AttachedMinion",
            take_no_damage=True
        ),
    ]

