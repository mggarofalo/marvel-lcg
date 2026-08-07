from . import *

# * Annabelle Riggs

def GetAbilities() -> Sequence['Ability']:

    def annabelle_riggs(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerDeckTop(
            effect,
            initiator,
            5,
            card_class="IdentitySpecific",
        )
        if face:
            initiator.GainCard(face, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            annabelle_riggs
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

