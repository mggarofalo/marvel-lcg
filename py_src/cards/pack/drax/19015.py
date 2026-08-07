from . import *

# Deflection

def GetAbilities() -> Sequence['Ability']:

    def deflection(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        value = min(message.will_take_damage, 5)
        value = initiator.DeclareNumber(1, value)
        message.PreventDamage(value, effect)
        initiator.DiscardDeckTopCards(value, effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.HeroInterrupt,
            Identity,
            deflection,
            is_from_attack=True,
        ).SetPlay().SetLabel()
    ]

