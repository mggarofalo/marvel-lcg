from . import *

# Plague and Pestilence

def GetAbilities() -> Sequence['Ability']:

    def plague_and_pestilence(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()
        TreatAsIfBlankUntilNextVillainPhase(player, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            plague_and_pestilence,
            has_defeating_player=True
        ),
    ]

