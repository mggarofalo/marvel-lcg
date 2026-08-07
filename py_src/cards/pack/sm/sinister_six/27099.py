from . import *

# * Vulture

def GetAbilities() -> Sequence['Ability']:

    def vulture_revealed(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardHandCards((1, 1), effect)
        MoveTheActiveCounterToTheNextVillain(effect)


    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "This",
            "You",
            vulture_revealed,
        ),
        SinisterSixWhenDefeated(),
    ]

