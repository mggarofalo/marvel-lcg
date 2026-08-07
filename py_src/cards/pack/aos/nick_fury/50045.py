from . import *

# Intelligence Analysis

def GetAbilities() -> Sequence['Ability']:

    def intelligence_analysis(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.CancelAllEffectsAndDiscardIt(effect)

    return [
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.Interrupt,
            "You",
            Treachery,
            "All",
            intelligence_analysis
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetCostFunc(CostFunc.RemoveTokens("YourSuitFormUpgrade", 1, 'threat')),
    ]

