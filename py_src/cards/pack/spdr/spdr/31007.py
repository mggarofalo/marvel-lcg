from . import *

# * Aunt May & Uncle Ben

def GetAbilities() -> Sequence['Ability']:

    def aunt_may_uncle_ben(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()

        faces = effect.cost_func.Get(CostFunc.Discard).return_discarded_cards
        faces = CardFinder(card_class="IdentitySpecific").Checks(faces)
        Faces.AddToHand(faces, initiator, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            aunt_may_uncle_ben,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Discard("YourPlayerDeckTop", range=(
            lambda effect: 3 if effect.GetInitiator().IsAlterEgo() else 2,
            lambda effect: 3 if effect.GetInitiator().IsAlterEgo() else 2,
        ))),
    ]

