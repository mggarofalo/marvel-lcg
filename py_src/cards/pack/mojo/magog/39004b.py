from . import *

# The Challengers

def GetAbilities() -> Sequence['Ability']:

    def the_challengers_revealed(effect: 'Effect', message: 'Message.AfterCardFlip') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        contender = Worlds.FindCardOnField(
            effect,
            name="Surprise Contender",
            card_type=Minion
        )
        if contender:
            Faces.GiveStatus([contender], "Tough", effect)
        else:
            first_player = Worlds.GetFirstPlayer(effect)
            face = Search.EncounterCard(
                effect,
                include_discard_pile=True,
                name="Surprise Contender",
                card_type=Minion,
            )
            if face:
                face.PutIntoPlay(first_player, effect)

    def the_challengers_boost(effect: 'Effect', message: 'Message.AfterCardPlacedCounter') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        Worlds.SetGameOver(True, effect)


    return [
        AbilityFactory.AfterFlipToThisFace(
            AbilityType.ForcedResponse,
            the_challengers_revealed
        ).SetName("Underdogs"),
        AbilityFactory.IfThereAreAtLeastCounterHere(
            "10*",
            'ratings',
            the_challengers_boost
        ),
    ]

