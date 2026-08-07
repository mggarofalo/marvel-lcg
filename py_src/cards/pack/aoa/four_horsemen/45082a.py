from . import *

# * Famine

def GetAbilities() -> Sequence['Ability']:

    def famine(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetAgainstPlayer()

        if player and this.health >= 1:
            player.DiscardDeckTopCards(10, effect)


    return [
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            "This",
            famine
        ),
        *ThisCannotBeDefeatedWhileAnotherVillainHas1HP()
    ]

