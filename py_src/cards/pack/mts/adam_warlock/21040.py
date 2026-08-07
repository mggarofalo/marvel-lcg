from . import *

# Quantum Magic

def GetAbilities() -> Sequence['Ability']:

    def quantum_magic(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.GainCard(effect.targets, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            quantum_magic
        ).SetPlay().SetLabel()
        .SetTarget(from_where=["YourDiscardPile"]),
    ]

