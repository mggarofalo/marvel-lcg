from . import *

# * Peter Quill

def GetAbilities() -> Sequence['Ability']:

    def peter_quill(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()

        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            name="Element Gun",
            card_type=Upgrade
        )
        if face:
            initiator.GainCard(face, effect)

    def smooth_talker(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.SwapWithDeckTop(effect.targets, initiator.player_deck, effect)

    return [
        AbilityFactory.WhenCardSetup(
            "This",
            peter_quill
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            smooth_talker,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().player_deck.GetSize() > 0,
            ]
        ).SetName("Smooth Talker")
        .SetTarget(from_where=["YourHandCards"])
        .LimitOncePerRound()
    ]

