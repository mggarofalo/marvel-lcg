from . import *

# Giant Strength

def GetAbilities() -> Sequence['Ability']:

    def giant_strength(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        for target in effect.targets:
            target.GainUntilTurnEnd(effect, attack=1)


    return [
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.HeroResponse,
            "You",
            giant_strength,
            to_form=CardFinder2("GIANT", Hero)
        ).SetTarget("YourHero")
    ]

