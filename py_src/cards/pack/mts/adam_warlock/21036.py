from . import *

# Cosmic Ward

def GetAbilities() -> Sequence['Ability']:

    def cosmic_ward(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.CancelWhenRevealedEffect(effect)
        Faces.DiscardAll([message.trigger], effect)
        Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.ForcedInterrupt,
            "You",
            Treachery,
            "WhenRevealed",
            cosmic_ward,
        ),
    ]

