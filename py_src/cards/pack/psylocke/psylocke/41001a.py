from . import *

# * Psylocke

def GetAbilities() -> Sequence['Ability']:

    def psylocke(effect: 'Effect', message: 'Message.WhenUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        Faces.FlipAllTo(effect.targets, None, effect)


    return [
        AbilityFactory.WhenUnitUseBasicPower(
            AbilityType.Interrupt,
            "This",
            psylocke
        ).SetName("Psi-Energy Control")
        .SetTarget(Upgrade, trait="PSI-ENERGY", canbe_flip=True, from_where=["YouControlCards"]),
    ]

