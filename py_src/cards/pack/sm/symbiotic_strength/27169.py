from . import *

# Unstable Sentience

def GetAbilities() -> Sequence['Ability']:

    def unstable_sentience_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        if villain:
            Faces.GiveFacedownBoostCards([villain], 1, effect)


    def unstable_sentience_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        message.GainBoostIcon(2, effect)
        message.would_atk_message.GainOverKill(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            unstable_sentience_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            unstable_sentience_boost,
            during_attack=True
        )
    ]

