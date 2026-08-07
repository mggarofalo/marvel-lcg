from . import *

# Light of Centuries Sphere

def GetAbilities() -> Sequence['Ability']:

    def light_of_centuries_sphere(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.defeating_player

        minion = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Minion
        )
        if minion and player:
            minion.PutIntoPlay(player, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            light_of_centuries_sphere,
        ),
    ]

