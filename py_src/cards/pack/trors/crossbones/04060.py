from . import *

# * Crossbones

def GetAbilities() -> Sequence['Ability']:

    def crossbones_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        RevealExperimentalWeaponsDeckTopCard(effect)


    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            conditions=[
                lambda effect, message:
                    effect.this.GetInventoryDeck().FindCardSize(CardFinder2("WEAPON", Attachment)) > 0,
            ],
            piercing=True
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            crossbones_revealed
        ),
    ]

