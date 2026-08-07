from . import *

# * Rally the Troops

def GetAbilities() -> Sequence['Ability']:

    def rally_the_troops(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)

        allies = Worlds.FindCardsOnField(
            effect,
            card_type=Ally
        )
        this.HealthUnits(allies, 2, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            rally_the_troops,
        ),
    ]

