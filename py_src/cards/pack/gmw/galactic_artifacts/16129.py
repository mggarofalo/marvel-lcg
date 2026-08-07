from . import *

# Philosopher's Stone

def GetAbilities() -> Sequence['Ability']:

    def philosophers_stone(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.killer.GetControlBy()
        if Player.IsType(player):
            player.MayChooseOneAbility(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Draw 2 cards",
                    lambda targets:
                        player.DrawUp(2, effect)
                ).SetTarget("YourIdentity")
            )

    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            philosophers_stone,
        ),
    ]

