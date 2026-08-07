from . import *

# Superpower Feedback

def GetAbilities() -> Sequence['Ability']:

    def superpower_feedback(effect: 'Effect', message: 'Message.AfterEffectResolved') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        identity.TakeDamage(this, 1, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity"
        ),
        AbilityFactory.AfterPlayerResolveAbility(
            AbilityType.ForcedResponse,
            "You",
            CardFinder(card_type=Identity)|
            CardFinder(card_class="IdentitySpecific"),
            superpower_feedback,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCostFunc(CostFunc.Discard("YourHandCards", card_class="IdentitySpecific")),
    ]

