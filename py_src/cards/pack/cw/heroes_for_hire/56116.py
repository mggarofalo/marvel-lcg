from . import *

# Heroes for Hire

def GetAbilities() -> Sequence['Ability']:

    def heroes_for_hire_defeated(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()
        team = Worlds.GetEnemyTeam(effect)

        face = Search.EncounterCard(
            effect,
            team,
            include_discard_pile=True,
            name="Unregistered Super",
            card_type=Obligation
        )
        if face:
            player.DealEncounterCard(face, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            heroes_for_hire_defeated,
        ),
    ]

