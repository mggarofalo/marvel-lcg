from . import *

# Mind Alteration

def GetAbilities() -> Sequence['Ability']:

    def mind_alteration(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        player.GetIdentity().TakeDamage(this, 1, effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity"
        ),
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.ForcedResponse,
            "AttachedPlayer",
            CardFinder(card_type=Event|Upgrade),
            mind_alteration,
        ),
        AbilityFactory.AfterUnitRecovery(
            AbilityType.Response,
            "You",
            DiscardThisCard
        ).SetCost(Cost("B")),
    ]

