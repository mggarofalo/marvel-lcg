from . import *

# * Thomas Edison

def GetAbilities() -> Sequence['Ability']:

    def thomas_edison(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> bool:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = this.GetEngagedPlayer()
        if player.engaged_minions.GetSize() > 1:
            return True
        return False

    return [
        #  [Players] cant deal damage to [Thomas Edison] while the engaged player has a minion also engaged with them
        AbilityFactory.ThisCannotTakeDamageWhile(
            conditions=[thomas_edison]
        ),
    ]

