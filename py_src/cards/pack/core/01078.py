from . import *

# Get Behind Me!

def GetAbilities() -> Sequence['Ability']:

    def get_behind_me(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        message.CancelWhenRevealedEffect(effect)
        villain = Worlds.FindVillain(effect)
        if villain:
            villain.DoAttackYou(initiator, effect)

    return [
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.HeroInterrupt,
            "AnyPlayer",
            Treachery,
            "WhenRevealed",
            get_behind_me,
            from_encounter=True,
        ).SetPlay()
    ]

