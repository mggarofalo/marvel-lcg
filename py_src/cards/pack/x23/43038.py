from . import *

# Predictable Ploy

def GetAbilities() -> Sequence['Ability']:

    def predictable_ploy(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.CancelWhenRevealedEffect(effect)


    return [
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.HeroInterrupt,
            "AnyPlayer",
            Treachery,
            "WhenRevealed",
            predictable_ploy
        ).SetPlay(only_if_there_is_side_scheme_in_victory=True).SetLabel(),
    ]

