from . import *

# Stephen Strange

def GetAbilities() -> Sequence['Ability']:

    def flip_top_card(player: 'Player', by_effect: 'Effect'):
        if player.additional_deck.GetSize() == 0:
            player.additional_deck.ShuffleWithDiscardPile(False, by_effect)
        # Faces.FlipAllTo(player.additional_deck.Get(), False)
        faces = player.additional_deck.Get()
        Faces.FlipAllTo(faces[:-1], False, by_effect)
        face = faces[-1].CastTo(Event)
        face.FlipTo(by_effect, face_up=True)

        # effect = player.GetIdentity().GetHeroFace().FindEffect2("Spell Mastery")
        # effect.ability.SetCost(face.GetPrintedCost())

    def move_to_additional_discard_pile(effect: 'Effect', message: 'Message.WhenCardWouldMoveToArea') -> None:
        this = effect.this.CastTo(Event)
        player = this.GetControlByPlayer()

        if player.discard_pile == message.into_area:
            message.ChangeToArea(player.additional_discard_pile, effect)

        if player.player_deck == message.into_area:
            message.ChangeToArea(player.additional_deck, effect)

    def stephen_strange(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this
        Unused(this)

        Faces.DiscardAll(effect.targets, effect)

    def with_invocation_deck(effect: 'Effect', message: 'Message.WhenPlayerSelectHero') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()

        def register_effect(face: 'CardFace'):
            face.effect.Registers(
                AbilityFactory.WhenCardWouldMoveToArea(
                    AbilityType.Temp0,
                    "This",
                    move_to_additional_discard_pile,
                    conditions=[
                        lambda effect, message:
                            initiator.discard_pile == message.into_area or \
                            initiator.player_deck == message.into_area
                    ]
                ),
            )

        for face in initiator.set_aside_deck.Get():
            register_effect(face)

        this.effect.Registers(
            AbilityFactory.AfterDeckShuffle(
                AbilityType.Temp0,
                lambda effect: initiator.additional_deck,
                lambda effect, message:
                    flip_top_card(initiator, effect),
            ),
            AbilityFactory.AfterCardsMoved(
                AbilityType.Temp0,
                initiator.set_aside_deck.Get(),
                lambda effect, message:
                    flip_top_card(initiator, effect),
                by_shuffle=False,
                conditions=[
                    lambda effect, message:
                        initiator.additional_deck not in message.into_areas
                        or \
                        initiator.additional_deck in message.into_areas and \
                        (
                            initiator.additional_deck in message.from_areas or \
                            initiator.additional_discard_pile in message.from_areas
                        )
                ]
            )
        )

        flip_top_card(initiator, effect)

    return [
        AbilityFactory.BeginGameWithSetAside([
                "09032",
                "09033",
                "09034",
                "09035",
                "09036",
            ],
            with_invocation_deck
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            stephen_strange
        ).SetName("Natural Talent")
        .SetTarget(SelectInvocationTopCard)
        .LimitOncePerPhase(),
    ]

