from . import *

# * Hobgoblin

def GetAbilities() -> Sequence['Ability']:

    def hobgoblin_revealed(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetToPlayer()
        player.GetIdentity().TakeIndirectDamage(this, 2, effect)
        MoveTheActiveCounterToTheNextVillain(effect)


    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "This",
            "You",
            hobgoblin_revealed,
        ),
        SinisterSixWhenDefeated(),
    ]

