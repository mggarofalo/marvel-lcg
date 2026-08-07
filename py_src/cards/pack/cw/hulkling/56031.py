from . import *

# * Altman Residence

def GetAbilities() -> Sequence['Ability']:

    def altman_residence(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        this.HealthUnits(effect.targets, 2, effect)

        Faces.ShuffleAllTo(effect.targets2, initiator.player_deck, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            altman_residence
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(name="Teddy Altman", canbe_heal=True)
        .SetTarget2(card_class="IdentitySpecific", from_where=["YourDiscardPile"]),
    ]

