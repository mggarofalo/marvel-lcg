from . import *

# "Bring the War!"

def GetAbilities() -> Sequence['Ability']:

    def bring_the_war_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player') -> List['CardFace']:
            face = player.DiscardControlCards(effect, control=True, finder=CardFinder(has_printed_res="G"))
            if face:
                return [face]
            return []
        faces: List['CardFace'] = Players.ForEachPlayer(effect, action, [])

        this.PlaceThreatOnSchemes([this], len(faces), effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            bring_the_war_revealed
        ),
    ]

