from . import *

# * Ronan the Accuser

def GetAbilities() -> Sequence['Ability']:

    def ronan_the_accuser_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        face = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            name="Cut the Power",
            card_type=SchemeSide2,
        )
        if face:
            face.Reveal(None, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            ronan_the_accuser_revealed
        ),
        GiveAdditionalBoostCardIfYouControlPowerStone(),
    ]

