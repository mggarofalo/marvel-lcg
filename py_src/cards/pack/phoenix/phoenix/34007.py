from . import *

# Telekinetic Shield

def GetAbilities() -> Sequence['Ability']:

    def telekinetic_shield(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.SetBeInstead(effect)

        damage = message.will_take_damage
        Faces.PlaceCountersOn([this], damage, 'damage', effect)
        if this.GetCounters('damage') >= 5:
            Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(Friend),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            "AttachedCharacter",
            telekinetic_shield,
            is_from_attack=True,
            who_deal_damage=Enemy,
        ),
    ]

