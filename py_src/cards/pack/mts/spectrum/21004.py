from . import *

# Pulsar

def GetAbilities() -> Sequence['Ability']:

    def pulsar(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.HealthUnits(effect.targets, 1, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Spectrum"),
            defense=2,
        ),
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.HeroResponse,
            "You",
            pulsar,
            to_this_additional_form=True,
        ).SetTarget("YourHero", canbe_heal=True),
    ]

