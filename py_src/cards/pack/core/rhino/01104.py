from . import *

# Hard to Keep Down

def GetAbilities() -> Sequence['Ability']:

    def hard_to_keep_down(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        if this.HealthUnits(effect.targets, 4, effect):
            pass
        else:
            this.GainSurge(1, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            hard_to_keep_down
        ).SetTarget(Villain)
    ]

