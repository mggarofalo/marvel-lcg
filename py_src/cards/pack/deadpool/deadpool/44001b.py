from . import *

# * Wade Wilson

def GetAbilities() -> Sequence['Ability']:

    def wade_wilson(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            card_class="IdentitySpecific",
            card_type=Event,
        )
        if face:
            initiator.GainCard(face, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            wade_wilson
        ).SetName("Break the Fourth Wall")
        .SetCostFunc(CostFunc.Discard("YourHandCards"))
        .LimitOncePerRound(),
    ]

