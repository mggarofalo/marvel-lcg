from . import *

# Animal Instinct

def GetAbilities() -> Sequence['Ability']:

    def animal_instinct(effect: 'Effect', message: 'Message.WhenUnitWouldThwart') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        value = message.trigger.CastTo(HasAttack).attack
        message.GainTHWForThisThwart(value, effect)

    return [
        AbilityFactory.WhenUnitMakeThwart(
            AbilityType.HeroInterrupt,
            CardFinder(name="X-23"),
            None,
            animal_instinct,
            is_basic_thwart=True
        ).SetPlay().SetLabel(),
    ]

