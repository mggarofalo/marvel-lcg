from . import *

# * Greer Nelson

def GetAbilities() -> Sequence['Ability']:

    def greer_nelson(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            name="Hunted"
        )
        if face:
            Faces.AddToHand([face], initiator, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            greer_nelson
        ).SetName("Undercover Work")
        .LimitOncePerRound(),
    ]

