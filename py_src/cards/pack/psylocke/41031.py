from . import *

# * Domino: Neena Thurman

def GetAbilities() -> Sequence['Ability']:

    def domino(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        face = initiator.player_deck.GetTop()
        if face:
            initiator.GainCard(face, effect)
            Faces.MoveAllTo(effect.targets, initiator.player_deck, effect)


    return [
        AbilityFactory.AfterUnitUseBasicPower(
            AbilityType.Response,
            "This",
            domino
        ).SetTarget(from_where=["YourHandCards"]),
    ]

