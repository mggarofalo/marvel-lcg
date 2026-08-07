from . import *

# * Venom

def GetAbilities() -> Sequence['Ability']:

    def venom_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        face = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            name="Tooth and Nail",
            card_type=SchemeSide2,
        )
        if face:
            face.PutIntoPlay("FirstPlayer", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            venom_revealed
        ),
        VengeanceRetribution(2)
    ]

