from . import *

# Frozen Solid

def GetAbilities() -> Sequence['Ability']:

    def frozen_solid(effect: 'Effect', message: 'Message.WhenEnemyWouldActivate') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.SetBeInstead(effect)
        Faces.DiscardAll([this], effect)

        player = this.GetOwnerPlayer()
        face = GetSetAsideFrostbite(player)

        if face:
            face.AttachTo2(message.trigger, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            Enemy
        ).SetPlay(hero_form_only=True),
        AbilityFactory.WhenEnemyWouldActivate(
            AbilityType.ForcedInterrupt,
            "AttachedEnemy",
            frozen_solid
        ),
    ]

