from . import *

# * Toad

def GetAbilities() -> Sequence['Ability']:

    def toad(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.attacked.GetControlByPlayer()
        player.DiscardRandomHandCards(1, effect)

    def toad_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardRandomHandCards(1, effect)


    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "This",
            "YouControlCharacter",
            toad,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            toad_boost
        ),
    ]

