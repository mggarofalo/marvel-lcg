from . import *

# Jackpot!

def GetAbilities() -> Sequence['Ability']:

    def jackpot(effect: 'Effect', message: 'Message.AfterCardDiscard') -> 'None':
        this = effect.this.CastTo(Resource)
        Unused(this)

        initiator = effect.GetInitiator()

        Faces.ShuffleAllTo([this], initiator.player_deck, effect)


    return [
        AbilityFactory.AfterCardDiscardInternal(
            AbilityType.Response,
            "This",
            jackpot,
            from_where="PlayerDeckTop",
        ),
    ]

