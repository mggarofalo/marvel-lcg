from . import *

# * Electro

def GetAbilities() -> Sequence['Ability']:

    def electro(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardDeckTopCards(7, effect)
        MoveTheActiveCounterToTheNextVillain(effect)


    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "This",
            "You",
            electro,
        ),
        SinisterSixWhenDefeated(),
    ]

