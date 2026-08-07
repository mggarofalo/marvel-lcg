from . import *

# * Black Widow

def GetAbilities() -> Sequence['Ability']:

    def black_widow(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        player = message.GetToPlayer()

        message.CancelAllEffectsAndDiscardIt(effect)

        # If you triggered Black Widow’s ability, then you are instructed to reveal another card from the encounter deck.
        Worlds.RevealEncounterDeckTopCard(player, effect)


    return [
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.Interrupt,
            "AnyPlayer",
            None,
            "All",
            black_widow,
            from_encounter=True,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("Trigger")
        .SetCost(Cost("B")),
    ]

