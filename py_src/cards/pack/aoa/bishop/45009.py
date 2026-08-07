from . import *

# Energy Conversion

def GetAbilities() -> Sequence['Ability']:

    def energy_conversion(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = initiator.discard_pile.FindCards(card_type=Resource)
        Faces.ShuffleAllTo(faces, initiator.player_deck, effect)
        
        this.effect.RegisterTemp(
            AbilityFactory.UnitCannotTakeDamageWhile(
                AbilityType.Temp0,
                "You",
                cannot_take_more_than_damage=3,
                is_from_attack=message,
            ),
            unregister_after_exec=False,
            until_event_end=message
        )


    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.HeroInterrupt,
            Enemy,
            energy_conversion
        ).SetPlay().SetLabel('defense')
        .SetTarget(Resource, from_where=["YourDiscardPile"], range="All")
        .SetHasNoTargetEffect(),
    ]

