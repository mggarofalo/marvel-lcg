from . import *

# Priority Target

def GetAbilities() -> Sequence['Ability']:

    def priority_target(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        player = message.GetKillerPlayer()
        if player:
            player.DrawUp(2, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(Enemy),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.Interrupt,
            "AttachedEnemy",
            priority_target
        ),
    ]

