from . import *

# Render Medical Aid

def GetAbilities() -> Sequence['Ability']:

    def render_medical_aid(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)

        Players.ForEachPlayer(
            effect,
            lambda player:
                player.AssignHeal(player.GetControlCharacters(), 5, effect)
        )


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            render_medical_aid,
        ),
    ]

