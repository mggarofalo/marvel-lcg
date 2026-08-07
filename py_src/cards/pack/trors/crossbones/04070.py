from . import *

# Crossbones' Assault

def GetAbilities() -> Sequence['Ability']:

    def crossbones_assault(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        crossbones = Worlds.FindCardOnField(
            effect,
            name="Crossbones",
            card_type=Villain
        )
        if crossbones:
            crossbones.DoActivate(player, effect)

    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            crossbones_assault,
            has_defeating_player=True
        ),
    ]

