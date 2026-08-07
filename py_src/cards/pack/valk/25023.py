from . import *

# * The Bifrost

def GetAbilities() -> Sequence['Ability']:

    def the_bifrost(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()

        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            trait="ASGARD",
            card_type=Ally,
            not_move=True
        )
        if face:
            initiator.PlayAnAlly(face, True)

    return [
        AbilityFactory.CanPlayThisSupportCard(
        ).SetPlay(only_if_your_identity_has_trait="ASGARD"),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            the_bifrost,
            # condition=lambda effect, message:
            #     has_ally_in_deck(effect),
        ).SetCostFunc(CostFunc.Exhaust("This"))
        # .SetTarget2(Select.From("YourPlayerDeck", trait="ASGARD", card_type=Ally, peek=True)),
    ]

