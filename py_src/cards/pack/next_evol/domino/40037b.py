from . import *

# * Neena Thurman

def GetAbilities() -> Sequence['Ability']:

    def neena_thurman(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.SwapWithDeckTop(effect.targets, initiator.discard_pile, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            neena_thurman
        ).SetTarget(from_where=["YourHandCards"])
        .LimitOncePerRound(),
    ]

