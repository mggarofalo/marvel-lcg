from . import *

# * Rogue: Anna Marie

def GetAbilities() -> Sequence['Ability']:

    def rogue(effect: 'Effect') -> int:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        charge = initiator.GetIdentity().GetCounters('charge')

        return charge


    return [
        AbilityFactory.ReduceCostToPlayThis(
            rogue
        ),
    ]

