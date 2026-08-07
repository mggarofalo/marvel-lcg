from . import *

# Flower Power

def GetAbilities() -> Sequence['Ability']:

    def flower_power(effect: 'Effect', message: 'Message.WhenGameOver'):
        groot = Worlds.FindCardOnField(
            effect,
            name="Groot",
            card_type=Identity
        )

        if not groot:
            return False
        if not groot.is_full_heath:
            return False
        if not groot.GetInventoryDeck().FindCard(name="Power Stone"):
            return False

    return [
        AbilityFactory.UsingOnlyHeroes(
            ["Groot"]
        ),
        AbilityFactory.WhenScenarioEnd(
            flower_power
        )
    ]

