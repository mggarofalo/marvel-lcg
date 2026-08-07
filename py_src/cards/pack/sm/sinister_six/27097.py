from . import *

# * Kraven the Hunter

def GetAbilities() -> Sequence['Ability']:

    def kraven_the_hunter_revealed(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardControlCards(effect, support=True, upgrade=True)
        MoveTheActiveCounterToTheNextVillain(effect)


    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "This",
            "You",
            kraven_the_hunter_revealed,
        ),
        SinisterSixWhenDefeated(),
    ]

