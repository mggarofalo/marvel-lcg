from . import *

# * Pestilence

def GetAbilities() -> Sequence['Ability']:

    def pestilence(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetAgainstPlayer()

        if player and this.health >= 1:
            TreatAsIfBlankUntilNextVillainPhase(player, effect)

    return [
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            "This",
            pestilence
        ),
        *ThisCannotBeDefeatedWhileAnotherVillainHas1HP()
    ]

