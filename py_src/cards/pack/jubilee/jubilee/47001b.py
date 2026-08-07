from . import *

# * Jubilation Lee

def GetAbilities() -> Sequence['Ability']:

    def jubilation_lee(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        scheme = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            name="Shopping Spree",
            card_type=PlayerSideScheme
        )
        if scheme:
            scheme.PutIntoPlay(initiator, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            jubilation_lee
        ).SetName("Mall Rat")
        .LimitOncePerPhase(),
    ]

