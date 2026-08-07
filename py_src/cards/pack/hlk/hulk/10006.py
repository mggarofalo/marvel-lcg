from . import *

# Unstoppable Force

def GetAbilities() -> Sequence['Ability']:

    def unstoppable_force(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)

        if effect.GetPaidResources().OnlyHasColor("R"):
            initiator = effect.GetInitiator()
            initiator.DrawUp(1, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            unstoppable_force,
        ).SetPlay()
        .SetTarget(name="Hulk", canbe_ready=True)
    ]

