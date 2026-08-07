from . import *

# Return the Favor

def GetAbilities() -> Sequence['Ability']:

    def return_the_favor(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 5, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            return_the_favor
        ).SetPlay().SetLabel('attack')
        .SetCostFunc(CostFunc.DiscardDeckTopUntil("EncounterDeck", Treachery, "Reveal"))
        .SetTarget(Villain),
    ]

