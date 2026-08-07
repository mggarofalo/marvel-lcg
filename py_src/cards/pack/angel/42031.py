from . import *

# Flying Formation

def GetAbilities() -> Sequence['Ability']:

    def flying_formation(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            flying_formation
        ).SetPlay().SetLabel()
        .SetTarget(Unit2, trait="AERIAL", canbe_ready=True, range=(1, 3)),
    ]

