from . import *

# Enhanced Spider-Sense

def GetAbilities() -> Sequence['Ability']:

    def enhanced_spider_sense(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.CancelWhenRevealedEffect(effect)

    return [
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.HeroInterrupt,
            "AnyPlayer",
            Treachery,
            "WhenRevealed",
            enhanced_spider_sense,
            from_encounter=True,
        ).SetPlay()
    ]

