from . import *

# Brimstone Dimension

def GetAbilities() -> Sequence['Ability']:

    def brimstone_dimension(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()
        face = Find.Find(
            effect,
            who_perform=player,
            name="Azazel",
        )
        if face:
            player.DealEncounterCard(face, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            brimstone_dimension,
            has_defeating_player=True
        ),
    ]

