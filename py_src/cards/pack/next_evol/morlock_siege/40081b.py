from . import *

# Routed

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ExpertModeOnly(),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Villain,
            get_new_value=lambda effect, face: effect.this.GetPlacedCardArea().FindCardSize(CardFinder(card_type=Villain)),
            retaliate=1,
        ),
        Routed()
    ]

