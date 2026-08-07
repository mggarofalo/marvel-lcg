from . import *

# * War

def GetAbilities() -> Sequence['Ability']:

    def war(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetAgainstPlayer()

        if player and this.health >= 1:
            player.DiscardControlCards(effect, upgrade=True, support=True)


    return [
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            "This",
            war
        ),
        *ThisCannotBeDefeatedWhileAnotherVillainHas1HP()
    ]

