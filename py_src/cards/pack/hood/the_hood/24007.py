from . import *

# Established Dominance

def GetAbilities() -> Sequence['Ability']:

    def established_dominance(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetAgainstPlayer()
        ResolveFoulPlayerAbility(player, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity",
        ),
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            Villain,
            established_dominance,
            conditions=[
                lambda effect, message:
                    message.trigger.IsName("The Hood")
            ],
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCostFunc(CostFunc.Exhaust("YourIdentity"))
        .SetCostFunc(CostFunc.PlaceThreatOn(2, card_type=MainScheme))
    ]

