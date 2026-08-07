from . import *

# * Klaw (II)

def GetAbilities() -> Sequence['Ability']:

    def klaw(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        face = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            name='The "Immortal" Klaw',
            card_type=SchemeSide2,
        )
        if face:
            face.Reveal(None, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            klaw
        ),
        GiveKlawAdditionalBoostCardWhenAttack()
    ]

