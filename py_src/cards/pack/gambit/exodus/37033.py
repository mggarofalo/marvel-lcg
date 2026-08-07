from . import *

# Herald of Avalon

def GetAbilities() -> Sequence['Ability']:

    def herald_of_avalon(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        face = Search.EncounterCard(
            effect,
            player,
            include_discard_pile=True,
            name="Exodus"
        )
        if face:
            face.Reveal(player, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            herald_of_avalon,
            has_defeating_player=True
        ),
    ]

