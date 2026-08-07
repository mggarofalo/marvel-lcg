from . import *

# Hujahdarian Monarch Egg

def GetAbilities() -> Sequence['Ability']:

    def hujahdarian_monarch_egg(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.killer.GetControlBy()
        if Player.IsType(player):
            player.MayChooseOneAbility(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Ready your identity",
                    lambda targets:
                        Faces.ReadyAll(targets, effect)
                ).SetTarget("YourIdentity", canbe_ready=True)
            )


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            hujahdarian_monarch_egg,
        ),
    ]

