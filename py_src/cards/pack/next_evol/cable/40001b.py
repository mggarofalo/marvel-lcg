from . import *

# * Nathan Summers

def GetAbilities() -> Sequence['Ability']:

    def nathan_summers(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            card_type=PlayerSideScheme,
        )
        if face:
            face.PutIntoPlay(initiator, effect)

    return [
        # You may include player side schemes from any aspect in your deck.
        AbilityFactory.WhenCardSetup(
            "This",
            nathan_summers
        ).SetName("Soldier X"),
    ]

