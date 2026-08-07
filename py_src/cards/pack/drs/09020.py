from . import *

# Unflappable

def GetAbilities() -> Sequence['Ability']:

    def unflappable(effect: 'Effect', message: 'Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        for target in effect.targets:
            player = target.GetControlByPlayer()
            player.DrawUp(1, effect)

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players"
        ),
        # TODO: The wording “after you defend against an attack and take no damage” means that you only check after the attack resolves, and you’re only checking to see if you take damage at that time. The effect does not check if you took damage during the attack.
        AbilityFactory.AfterUnitDefendAgainstAttack(
            AbilityType.Response,
            "You",
            unflappable,
            take_no_damage=True,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("Initiator")
    ]

