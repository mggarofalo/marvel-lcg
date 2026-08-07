from . import *

# Sisterhood

def GetAbilities() -> Sequence['Ability']:

    def sisterhood(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            name="Honey Badger"
        )
        if face:
            Faces.AddToHand([face], initiator, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            sisterhood
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Discard("YourHandCards", card_class="IdentitySpecific")),
    ]

