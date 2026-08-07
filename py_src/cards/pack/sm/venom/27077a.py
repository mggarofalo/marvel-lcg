from . import *

# Bell Tower

def GetAbilities() -> Sequence['Ability']:

    def bell_tower(effect: 'Effect', message: 'Message.AfterCardPlacedCounter') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        this.card.Flip(effect)

    def bell_tower_interrupt(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        message.SetBeInstead(effect)
        value = message.will_take_damage
        Faces.PlaceCountersOn([this], value, 'chime', effect)


    return [
        AbilityFactory.IfThereAreAtLeastCounterHere(
            "3*",
            'chime',
            bell_tower,
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.Interrupt,
            CardFinder(name="Venom"),
            bell_tower_interrupt,
            is_from_attack=True,
        ),
    ]

