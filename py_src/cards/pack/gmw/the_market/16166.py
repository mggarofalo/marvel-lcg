from . import *

# Ardent Resolve

def GetAbilities() -> Sequence['Ability']:

    def ardent_resolve(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            ardent_resolve
        ).SetPlay().SetLabel()
        .SetTarget(Friend)
        .SetHasNoTargetEffect(),
    ]

