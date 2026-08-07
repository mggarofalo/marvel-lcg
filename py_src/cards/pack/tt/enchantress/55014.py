from . import *

# Love Concoction

def GetAbilities() -> Sequence['Ability']:

    def love_concoction(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.PlaceCountersOn(effect.targets, 1, 'charm', effect)
        Faces.ReadyAll(effect.targets2, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity"
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.ForcedAction,
            love_concoction
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(CardFinder(trait="ENCHANTMENT"), from_where=["YourPlayArea"])
        .SetTarget2("YourIdentity", canbe_ready=True),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCostFunc(CostFunc.Exhaust("This")),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            RevealThisCard
        ),
    ]

