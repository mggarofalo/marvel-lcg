from . import *

# * Ant-Man

def GetAbilities() -> Sequence['Ability']:

    def ant_man(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 1, effect)

    return [
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.Response,
            "You",
            ant_man,
            to_this_form=True,
        ).SetTarget(Scheme2)
        .SetName("Puny Pest")
    ]

