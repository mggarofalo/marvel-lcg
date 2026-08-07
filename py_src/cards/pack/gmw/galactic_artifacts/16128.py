from . import *

# Magical Teapot

def GetAbilities() -> Sequence['Ability']:

    def magical_teapot(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.killer.GetControlBy()
        if Player.IsType(player):
            player.MayChooseOneAbility(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Heal 4 damage from your identity",
                    lambda targets:
                        this.HealthUnits(targets, 4, effect)
                ).SetTarget("YourIdentity", canbe_heal=True)
            )

    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            magical_teapot,
        ),
    ]

