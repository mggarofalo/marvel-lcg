from . import *

# City Under Attack

def GetAbilities() -> Sequence['Ability']:

    def city_under_attack(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()
        player.DrawUp(1, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            city_under_attack,
            has_defeating_player=True
        ),
    ]

