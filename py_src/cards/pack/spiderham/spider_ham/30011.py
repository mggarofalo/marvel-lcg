from . import *

# Organic Webbing

def GetAbilities() -> Sequence['Ability']:

    def organic_webbing(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)

        for unit in effect.targets:
            unit.GainUntilPhaseEnd(effect, trait="AERIAL")


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Spider-Ham"),
            thwart=1,
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            organic_webbing,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter(Select.From(None, finder=CardFinder(name="Spider-Ham", canbe_ready=True)), 1, 'toon'))
        .SetTarget("YourHero"),
    ]

