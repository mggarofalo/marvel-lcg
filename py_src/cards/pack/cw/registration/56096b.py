from . import *

# S.H.I.E.L.D. Recruits

def GetAbilities() -> Sequence['Ability']:

    def shield_recruits_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        team = Worlds.GetEnemyTeam(effect)
        faces = Search.EncounterCards(
            effect,
            team,
            include_discard_pile=False,
            total="1*",
            card_type=Minion
        )

        def action(player: 'Player'):
            if faces:
                face = faces.pop()
                player.DealEncounterCard(face, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.ThisGainKeyword(
            check_fn=lambda effect, ui:
                Worlds.GetYourTeamSize(effect) > 1,
            hinder="2*",
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            shield_recruits_revealed
        ),
    ]

