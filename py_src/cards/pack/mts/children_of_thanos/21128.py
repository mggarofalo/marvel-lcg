from . import *

# Tribute

def GetAbilities() -> Sequence['Ability']:

    def tribute(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()
        player.DealEncounterCards(1, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            tribute,
            has_defeating_player=True
        ),
    ]

