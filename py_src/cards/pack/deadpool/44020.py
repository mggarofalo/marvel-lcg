from . import *

# Get Rage-y

def GetAbilities() -> Sequence['Ability']:

    def get_rage_y(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)
        for target in effect.targets:
            target.GainUntilPhaseEnd(effect, attack=1)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            get_rage_y
        ).SetPlay().SetLabel()
        .SetTarget(Ally, canbe_ready=True),
    ]

