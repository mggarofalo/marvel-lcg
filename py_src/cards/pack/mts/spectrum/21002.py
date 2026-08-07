from . import *

# Gamma

def GetAbilities() -> Sequence['Ability']:

    def gamma(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Spectrum"),
            attack=2,
        ),
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.HeroResponse,
            "You",
            gamma,
            to_this_additional_form=True,
        ).SetTarget(Enemy),
    ]

