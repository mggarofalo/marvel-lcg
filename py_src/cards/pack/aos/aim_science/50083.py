from . import *

# A.I.M. Scientist

def GetAbilities() -> Sequence['Ability']:

    def aim_scientist(effect: 'Effect', message: 'Message.CheckIfUnitCanBeAttackBy') -> bool:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.attacker.GetControlByPlayer()
        if this.GetEngagedPlayer() == player and len(player.GetEngagedMinions()) > 1:
            return True
        return False

    return [
        *AbilityFactory.UnitCannotAttackTarget(
            None,
            cannot_attack="This",
            conditions=[
                aim_scientist
            ]
        ),
    ]

