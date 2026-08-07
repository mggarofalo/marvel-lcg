from . import *

# Kree Manipulator

def GetAbilities() -> Sequence['Ability']:

    def kree_manipulator(effect: 'Effect', message: 'Message.WhenCardRevealed|Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        scheme = Worlds.FindMainScheme(this)
        if scheme:
            this.PlaceThreatOnSchemes([scheme], 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            kree_manipulator
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            kree_manipulator,
            is_undefended_attack=True,
        )
    ]

