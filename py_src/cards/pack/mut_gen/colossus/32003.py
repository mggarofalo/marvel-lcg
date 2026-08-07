from . import *

# * Piotr's Studio

def GetAbilities() -> Sequence['Ability']:

    def piotrs_studio(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        face = initiator.DiscardDeckUntil(
            effect,
            card_class="IdentitySpecific",
        )
        if face:
            initiator.GainCard(face, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            piotrs_studio
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

