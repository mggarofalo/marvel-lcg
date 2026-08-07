from . import *

# Alluring Call

def GetAbilities() -> Sequence['Ability']:

    def alluring_call(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.PlaceCountersOn(effect.targets, 1, 'charm', effect)
        initiator.PlayCardsLikeInTurn(effect.targets2, effect, update_resources_cost=-1)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity"
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.ForcedAction,
            alluring_call
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(CardFinder(trait="ENCHANTMENT"), from_where=["YourPlayArea"])
        .SetTarget2("YourHandCards", canbe_play=True),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCostFunc(CostFunc.Exhaust("This")),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            RevealThisCard
        ),
    ]

