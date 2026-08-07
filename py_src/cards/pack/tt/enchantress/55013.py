from . import *

# Kiss of Temptation

def GetAbilities() -> Sequence['Ability']:

    def kiss_of_temptation(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.PlaceCountersOn(effect.targets, 1, 'charm', effect)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)
        initiator.DiscardHandCards((1, 1), effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity"
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.ForcedAction,
            kiss_of_temptation
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(CardFinder(trait="ENCHANTMENT"), from_where=["YourPlayArea"])
        .SetTarget2("This"),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCostFunc(CostFunc.Exhaust("This")),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            RevealThisCard
        ),
    ]

