from . import *

# Safeguard

def GetAbilities() -> Sequence['Ability']:

    def safeguard(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Tough", effect)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            safeguard
        ).SetPlay().SetLabel()
        .SetTarget(Friend, range=(0, 2), canbe_tough=True)
        .SetHasNoTargetEffect(),
    ]

