from . import *

# Organizational Support

def GetAbilities() -> Sequence['Ability']:

    def organizational_support(effect: 'Effect', message: 'Message.CheckPlayerCanPayCost') -> 'Resources|None':
        this = effect.this.CastTo(Resource)
        Unused(this)

        initiator = effect.GetInitiator()
        identity = initiator.GetIdentity()

        faces = initiator.GetControlCardsByType(CardFinder(canbe_exhaust=True, share_trait=identity), ally=True, support=True)
        return FacesCounter.GetPrintedResources([this] + faces)

    def organizational_support_res(effect: 'Effect', message: 'Message.WhenPlayerPayingResources') -> 'Resources':
        this = effect.this.CastTo(Resource)
        Unused(this)

        faces = effect.cost_func.Get(CostFunc.Exhaust).return_exhausted_cards
        return FacesCounter.GetPrintedResources([this] + faces)

    return [
        AbilityFactory.DoDiscardThisToGenerateResources(
            AbilityType.Interrupt,
            res_fn=organizational_support_res,
        ).SetCostFunc(CostFunc.Exhaust(
            card_type=Support|Ally,
            share_trait_with_your_identity=True,
            from_where=["YouControlCards"],
            range=(0, 3))),
        AbilityFactory.CheckThisCanDropPay(
            organizational_support,
        ),
    ]

