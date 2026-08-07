from . import *

# Perseverance

def GetAbilities() -> Sequence['Ability']:

    def perseverance(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Tough", effect)


    return [
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.HeroResponse,
            "You",
            perseverance
        ).SetPlay().SetLabel()
        .SetTarget("YourIdentity", canbe_tough=True),
    ]

