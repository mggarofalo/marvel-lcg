from . import *

# * Gamora

def GetAbilities() -> Sequence['Ability']:

    def gamora(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = initiator.LookAtDeck(initiator.player_deck, 1, effect)
        faces = CardFinder(traits=["ATTACK", "THWART"], card_type=Event).Checks(faces)
        Faces.AddToHand(faces, initiator, effect)


    return [
        # Skilled Tactician — You may include up to 6 [[attack]] and [[thwart]] events in your deck from aspects other than your own.
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            gamora
        ).LimitOncePerRound(),
    ]

