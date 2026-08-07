from . import *

# * Drang

def GetAbilities() -> Sequence['Ability']:

    def drang_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        face = Worlds.FindCardOnField(
            effect,
            name=DRANGS_SPEAR,
            card_type=Attachment
        )
        if face:
            Faces.GiveFacedownBoostCards([this], 1, effect)
        else:
            face = Search.EncounterCard(
                effect,
                include_discard_pile=True,
                name=DRANGS_SPEAR,
                card_type=Attachment
            )
            if face:
                face.Reveal(None, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            drang_revealed
        ),
        ResolveBadoonShipChargeUpabilityAfterThisScheme(),
    ]

