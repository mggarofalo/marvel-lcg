from . import *

# Advanced Glider

def GetAbilities() -> Sequence['Ability']:

    def advanced_glider(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Attachment)
        villain = this.GetBindFace().CastTo(Villain)

        player = message.GetAgainstPlayer()
        villain.DoActivate(player, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            "AttachedVillain",
            advanced_glider,
        ).LimitOncePerRoundPerPlayer(),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCostFunc(CostFunc.Discard(
            "YourHandCards",
            trait="ATTACK",
            combined_resource_cost=(3, Math.INT_INF)
        ))
    ]

