from . import *

# Stark Tower

def GetAbilities() -> Sequence['Ability']:

    def stark_tower_defeated(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        team = Worlds.GetEnemyTeam(effect)
        face = Search.EncounterCard(
            effect,
            team,
            include_discard_pile=True,
            card_type=Attachment,
            set_name="Iron Man"
        )
        if face:
            player = Worlds.GetYourTeam(effect)
            face.Reveal(player, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            stark_tower_defeated,
        ),
    ]

