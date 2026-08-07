from . import *

# Secret Avengers

def GetAbilities() -> Sequence['Ability']:

    def secret_avengers_defeated(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        team = Worlds.GetEnemyTeam(effect)
        face = Search.EncounterCardTop(
            effect,
            team,
            5
        )
        if face:
            leader = Worlds.GetEnemyLeader(effect)
            if leader:
                leader.GiveBoostCard(face, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            secret_avengers_defeated,
        ),
    ]

