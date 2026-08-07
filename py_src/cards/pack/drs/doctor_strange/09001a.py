from . import *

# * Doctor Strange

def GetAbilities() -> Sequence['Ability']:

    def doctor_strange(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.ResolveSpecialAbility(effect.targets, effect)

    # def exhaust_and_pay(effect: 'Effect') -> bool:
    #     return CostFunc.ExhaustThis(effect) and PayCostOfInvocationTopCard(effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            doctor_strange,
            conditions=[
                lambda effect, message:
                    InvocationTopCardHasTarget(effect, message)
            ],
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCost(GetCostOfInvocationTopCard)
        .SetTarget(SelectInvocationTopCard)
        .SetName("Spell Mastery")
    ]

