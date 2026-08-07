from . import *

# * Phoenix: Jean Grey

def GetAbilities() -> Sequence['Ability']:

    def phoenix(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.GainCard(effect.targets, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            phoenix
        ).SetTarget(card_class="IdentitySpecific", from_where=["YourDiscardPile"]),
    ]

