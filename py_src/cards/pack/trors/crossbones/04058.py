from . import *

# * Crossbones

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            conditions=[
                lambda effect, message:
                    effect.this.GetInventoryDeck().FindCardSize(CardFinder2("WEAPON", Attachment)) > 0,
            ],
            piercing=True
        )
    ]

