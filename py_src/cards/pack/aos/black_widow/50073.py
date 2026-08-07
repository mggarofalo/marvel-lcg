from . import *

# A.I.M. Grunt

def GetAbilities() -> Sequence['Ability']:

    def aim_grunt_perparation(effect: 'Effect', message: 'Message.WhenResolvePreparationAbility') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        if player:
            this.PutIntoPlay(player, effect)
        message.ResolveThisAttackToInstead(this, effect)


    return [
        AbilityFactory.WhenResolvePreparationAbility(
            "This",
            aim_grunt_perparation
        ),
    ]

