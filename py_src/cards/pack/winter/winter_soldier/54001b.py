from . import *

# * Bucky Barnes

def GetAbilities() -> Sequence['Ability']:

    def bucky_barnes(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()

        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            name="Cybernetic Arm"
        )
        if face:
            face.PutIntoPlay(initiator, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            bucky_barnes
        ).SetName("Cybernetically Enhanced")
        .SetCost(Cost("1")),
    ]

