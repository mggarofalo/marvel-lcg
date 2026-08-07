from . import *

# Hard as Nails

def GetAbilities() -> Sequence['Ability']:

    def hard_as_nails(effect: 'Effect', message: 'Message.WhenCardRevealed|Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        for target in effect.targets:
            villain = target.CastTo(Villain)
            if not Faces.GiveStatus([villain], "Tough", effect):
                this.HealthUnits([villain], 3, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            hard_as_nails
        ).SetTarget(Villain),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            hard_as_nails
        ).SetTarget(Villain),
    ]

