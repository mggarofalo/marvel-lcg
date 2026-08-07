from . import *

# Advanced Suit

def GetAbilities() -> Sequence['Ability']:

    def advanced_suit(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit|Message.AfterUnitDefeatedScheme') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = effect.cost_func.Get(CostFunc.Discard).return_discarded_cards
        value = FacesCounter.GetPrintedResourcesIcon(faces, initiator.hand_cards)
        this.HealthUnits(effect.targets, value, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            CardFinder(traits=["X-FORCE", "X-MEN"], card_type=Ally),
        ),
        AbilityFactory.AfterUnitDefeatedUnit(
            AbilityType.Response,
            "AttachedAlly",
            Minion,
            advanced_suit,
        ).SetCostFunc(CostFunc.Discard("YourHandCards"))
        .SetTarget("Trigger", canbe_heal=True),
        AbilityFactory.AfterUnitDefeatedScheme(
            AbilityType.Response,
            "AttachedAlly",
            SchemeSide2,
            advanced_suit,
        ).SetCostFunc(CostFunc.Discard("YourHandCards"))
        .SetTarget("Trigger", canbe_heal=True),
    ]

