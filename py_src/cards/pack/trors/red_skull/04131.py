from . import *

# Hydra Exo-Soldier

def GetAbilities() -> Sequence['Ability']:

    def hydra_exo_soldier_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        if villain:
            Faces.GiveStatus([villain], "Tough", effect)
            Faces.GiveFacedownBoostCards([villain], 1, effect)


    return [

        AbilityFactory.WhenCardBecomeBoost(
            "This",
            hydra_exo_soldier_boost
        ),
    ]

