from . import *

# Reload

def GetAbilities() -> Sequence['Ability']:

    def reload(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            reload
        ).SetPlay().SetLabel()
        .SetTarget(Upgrade, trait="TECH", canbe_ready=True, from_where=["YouControlCards"], range="All")
    ]

