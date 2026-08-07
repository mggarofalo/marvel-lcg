from . import *

# * Thanos

def GetAbilities() -> Sequence['Ability']:

    def thanos_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        face = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            name="Thanos's Armor",
            card_type=Attachment
        )
        if face:
            face.Reveal(None, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            thanos_revealed
        ),
        AfterTheinfinityStoneDeckRunsOutGiveThisFacedownBoostCard(1)
    ]

