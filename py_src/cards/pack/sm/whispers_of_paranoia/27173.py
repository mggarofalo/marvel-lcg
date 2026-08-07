from . import *

# Analysis Paralysis

def GetAbilities() -> Sequence['Ability']:

    def analysis_paralysis_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()

        player = message.GetToPlayer()
        face = Search.EncounterCard(
            effect,
            player,
            include_discard_pile=True,
            include_set_aside=True,
            card_type=SchemeSide2,
            is_nemesis=player,
        )
        if face:
            face.Reveal(player, effect)
            this.PlaceThreatOnSchemes([this], face.threat, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            analysis_paralysis_revealed
        ),
    ]

