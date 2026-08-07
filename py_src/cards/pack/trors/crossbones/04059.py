from . import *

# * Crossbones

def GetAbilities() -> Sequence['Ability']:

    def crossbones_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        face = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            name="Crossbones' Machine Gun",
            card_type=Attachment,
        )
        if face:
            face.AttachTo2(effect.this, effect)


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

