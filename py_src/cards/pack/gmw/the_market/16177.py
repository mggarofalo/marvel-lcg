from . import *

# Triple Threat

def GetAbilities() -> Sequence['Ability']:

    def triple_threat(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)
        Faces.RemoveAllFromGame([this], effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            triple_threat
        ).SetPlay().SetLabel()
        .SetTarget(Unit2, canbe_ready=True, range=(1, 3))
        .SetHasNoTargetEffect(),
    ]

