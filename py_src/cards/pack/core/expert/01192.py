from . import *

# Masterplan

def GetAbilities() -> Sequence['Ability']:

    def masterplan(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        had_placed = False
        for scheme in Worlds.GetSideSchemes(effect):
            this.PlaceThreatOnSchemes([scheme], 4, effect)
            had_placed = True
        if not had_placed:
            face = Worlds.DiscardEncounterCardsUntil(
                effect,
                card_type=SchemeSide2
            )
            if face:
                face.Reveal(message.to_player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            masterplan
        )
    ]
