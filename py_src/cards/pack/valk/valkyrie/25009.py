from . import *

# Visit Valhalla

def GetAbilities() -> Sequence['Ability']:

    def visit_valhalla(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ReturnToHand(effect.targets, initiator, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            visit_valhalla
        ).SetPlay().SetLabel()
        .SetTarget(card_class="IdentitySpecific", from_where=["YourDiscardPile"])
    ]

