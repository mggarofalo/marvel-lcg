from . import *

# Spoiling for a Fight

def GetAbilities() -> Sequence['Ability']:

    def spoiling_for_a_fight(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            spoiling_for_a_fight
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.DiscardDeckTopUntil("EncounterDeck", Minion, "PutIntoPlayEngagedYou"))
        .SetTarget("YourHero", canbe_ready=True),
    ]

