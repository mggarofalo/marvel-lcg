from . import *

# Bell Tower

def GetAbilities() -> Sequence['Ability']:

    def bell_tower(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        value = message.will_take_damage
        chime = this.GetCounters('chime')
        value = min(value, chime)
        Faces.RemoveCountersOn([this], value, 'chime', effect)
        message.PreventDamage(value, effect)

    def bell_tower_flip(effect: 'Effect', message: 'Message.AfterCardRemovedCounter') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        this.card.Flip(effect)

    return [
        AbilityFactory.IncreaseAllDamageUnitTakeBy(
            AbilityType.NonKeyword,
            CardFinder(name="Venom"),
            1
        ),
        AbilityFactory.IfThereIsNoCounterHere(
            'chime',
            bell_tower_flip
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            # Send.WhenUnitWouldDealDamage,?
            Identity,
            bell_tower,
            is_from_attack=True,
            who_deal_damage=CardFinder(name="Venom")
        ),
    ]

