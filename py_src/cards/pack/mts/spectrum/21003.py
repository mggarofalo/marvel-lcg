from . import *

# Photon

def GetAbilities() -> Sequence['Ability']:

    def photon(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 1, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Spectrum"),
            thwart=2,
        ),
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.HeroResponse,
            "You",
            photon,
            to_this_additional_form=True,
        ).SetTarget(Scheme2),
    ]

