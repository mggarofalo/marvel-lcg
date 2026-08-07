from . import *

# * Jessica Drew's Apartment

def GetAbilities() -> Sequence['Ability']:

    def jessica_drews_apartment(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerDeckTop(
            effect,
            initiator,
            5,
            card_class="Aspect",
        )
        if face:
            initiator.GainCard(face, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            jessica_drews_apartment
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

