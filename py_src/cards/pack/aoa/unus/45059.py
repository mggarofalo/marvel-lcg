from . import *

# * Unus

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                GetThreatOnGenePool(effect) >= 3,
            retaliate=1,
            change_on_event=OnEvent.Threat(CardFinder(name="Gene Pool"))
        ),
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                GetThreatOnGenePool(effect) >= 6,
            stalwart=1,
            change_on_event=OnEvent.Threat(CardFinder(name="Gene Pool"))
        ),
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                GetThreatOnGenePool(effect) >= 9,
            amplify=1,
            change_on_event=OnEvent.Threat(CardFinder(name="Gene Pool"))
        ),
    ]

