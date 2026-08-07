from . import *

# A.I.M. Commando

def GetAbilities() -> Sequence['Ability']:

    def aim_commando_preparation(effect: 'Effect', message: 'Message.WhenResolvePreparationAbility') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        def action():
            player = message.GetToPlayer()
            if player:
                this.PutIntoPlay(player, effect)

        message.AfterThisAttack(action)


    return [
        AbilityFactory.WhenResolvePreparationAbility(
            "This",
            aim_commando_preparation
        ),
    ]

