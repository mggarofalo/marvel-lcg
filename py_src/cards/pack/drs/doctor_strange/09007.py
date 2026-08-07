from . import *

# Protective Ward

def GetAbilities() -> Sequence['Ability']:

    def protective_ward(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this = effect.this.CastTo(Event)
        message.CancelAllEffectsAndDiscardIt(effect)


    return [
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.HeroInterrupt,
            "AnyPlayer",
            Treachery,
            "All",
            protective_ward,
            from_encounter=True,
        ).SetPlay(),
    ]

