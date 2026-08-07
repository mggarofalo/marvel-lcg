from . import *

# Stun Net

def GetAbilities() -> Sequence['Ability']:

    def stun_net_preparation(effect: 'Effect', message: 'Message.WhenResolvePreparationAbility') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        def action():
            attacker = message.attacker
            if attacker:
                this.AttachTo2(attacker, effect)

        message.AfterThisAttack(action)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity"
        ),
        *AbilityFactory.UnitCannotAttackTarget(
            "AttachedCharacter",
            cannot_trigger_attack_ability=True,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCostFunc(CostFunc.Exhaust("YouControlUnit"))
        .AnyPlayerCanDoThis(),
        AbilityFactory.WhenResolvePreparationAbility(
            "This",
            stun_net_preparation
        ),
    ]

