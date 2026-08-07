from . import *

# Infinite Soldier

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                GetThreatOnGenePool(effect) >= 3 and Minion.IsType(effect),
            quickstrike=True,
            change_on_event=[OnEvent.Threat(CardFinder(name="Gene Pool")), OnEvent.CardType("This")]
        ),
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                GetThreatOnGenePool(effect) >= 6 and Minion.IsType(effect),
            surge=True,
            change_on_event=[OnEvent.Threat(CardFinder(name="Gene Pool")), OnEvent.CardType("This")]
        ),
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                GetThreatOnGenePool(effect) >= 9 and Minion.IsType(effect),
            health=3,
            change_on_event=[OnEvent.Threat(CardFinder(name="Gene Pool")), OnEvent.CardType("This")]
        ),
    ]

