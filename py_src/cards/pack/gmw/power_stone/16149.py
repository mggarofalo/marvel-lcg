from . import *

# * Power Stone

def GetAbilities() -> Sequence['Ability']:

    def power_stone(effect: 'Effect', message: 'Message.AfterFaceDealDamage|Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        this.AttachTo2(message.trigger, effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        *AbilityFactory.AfterUnitDealsDamageWithSingleAttack(
            AbilityType.ForcedResponse,
            CardFinder(card_type=Hero|Villain),
            "AttachedCharacter",
            power_stone,
            dealt_more_than_damage=3,
        ),
        # Q: If an identity who controls the Power Stone is defeated, what happens to the Power Stone?
        # A: If a player is eliminated from the game while a permanent card they do not own is in their play area, place that permanent card in its owner’s discard pile. In the case of the Power Stone, place it in the encounter discard pile
    ]

