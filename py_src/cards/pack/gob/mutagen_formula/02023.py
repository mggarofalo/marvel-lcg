from . import *

# Goblin Soldier

def GetAbilities() -> Sequence['Ability']:

    def goblin_soldier(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = this.GetEngagedPlayer()
        identity = player.GetIdentity()
        this.DealDamage([identity], 1, effect)

    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            goblin_soldier
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            PutThisIntoPlay,
        )
    ]

