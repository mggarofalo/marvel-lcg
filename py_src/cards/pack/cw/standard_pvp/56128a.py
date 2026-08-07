from . import *

# Choosing Sides

def GetAbilities() -> Sequence['Ability']:

    def choosing_sides(effect: 'Effect', message: 'Message.AfterCardRemovedToken') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        team = Worlds.GetEnemyTeam(effect)
        faces = Search.EncounterCardsTop(
            effect,
            team,
            5,
            total="1*"
        )

        def action(player: 'Player'):
            face = faces.pop()
            player.DealEncounterCard(face, effect)

        Players.ForEachPlayer(effect, action)

        this.card.Flip(effect)


    return [
        AbilityFactory.UnitCannotTakeDamageWhile(
            AbilityType.NonKeyword,
            "EnemyLeader",
            cannot_take_more_than_damage=2,
            is_from_attack=True
        ),
        AbilityFactory.AfterLastThreatIsRemovedFromHere(
            AbilityType.ForcedResponse,
            choosing_sides,
        ),
    ]

