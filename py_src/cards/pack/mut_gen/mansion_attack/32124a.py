from . import *

# * Toad A

def GetAbilities() -> Sequence['Ability']:

    def toad(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.attacked.GetControlByPlayer()
        player.DiscardRandomHandCards(1, effect)


    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "This",
            "YouControlCharacter",
            toad,
        ),
    ]

