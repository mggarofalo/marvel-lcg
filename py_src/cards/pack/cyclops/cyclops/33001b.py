from . import *

# * Scott Summers

def GetAbilities() -> Sequence['Ability']:

    def scott_summers(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            trait="TACTIC",
            card_type=Upgrade,
        )
        if face:
            initiator.GainCard(face, effect)


    return [
        # You may include [[X-MEN]] allies from any aspect in your deck.
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            scott_summers
        ).SetName("Constant Training")
        .LimitOncePerRound(),
    ]

