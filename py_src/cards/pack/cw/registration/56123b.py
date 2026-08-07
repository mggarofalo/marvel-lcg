from . import *

# The Initiative

def GetAbilities() -> Sequence['Ability']:

    def the_initiative_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        team = Worlds.GetEnemyTeam(effect)
        faces = Search.EncounterCards(
            effect,
            team,
            include_discard_pile=True,
            total="1*",
            card_type=SchemeSide2
        )

        def action(player: 'Player'):
            face = faces.pop()
            player.DealEncounterCard(face, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_initiative_revealed
        ),
    ]

