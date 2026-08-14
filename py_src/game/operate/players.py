from . import *

class Players:

    @staticmethod
    # Only work for Treachery
    def CannotChangeFormDuringNextTurn(player: 'Player', effect: 'Effect'):
        from game.operate.effects import Effects

        identity = player.GetIdentity()
        def action():
            player.has_change_form = True
            cannot_effects = identity.effect.Registers(
                AbilityFactory.PlayersCannotChangeForms(AbilityType.Temp0, "You"),
            )
            identity.effect.RegisterTemp(
                AbilityFactory.WhenPlayerTurnEnd(
                    AbilityType.Temp0,
                    "This",
                    lambda effect, message:
                        Effects.UnRegister(cannot_effects)
                ),
                unregister_after_exec=True,
            )

        identity.effect.RegisterTemp(
            AbilityFactory.WhenPlayerTurnBegin(
                AbilityType.Temp0,
                "This",
                lambda effect, message:
                    action()
            ),
            unregister_after_exec=True,
        )

    # The label the hand discard is offered under. Same rule as
    # `DISCARD_ATTACHMENT_PROMPT` below and `SearchInternal.MAY_SEARCH_PROMPT`
    # (MARVEL-112): nameless, this one rendered as the binding effect's name, so
    # 02041 Power Drain asked "When_Defeated?" and 02047 Running Interference
    # asked "Forced_Response?" for what is a hand discard. The icon count is in
    # the label because it is the whole constraint on the answer -- which cards
    # are legal is decided by it -- and because the house style for a choice
    # label is the printed clause, amount included ("Place 2 threat here").
    DISCARD_RESOURCE_ICONS_PROMPT = "Discard {size} resource icons from your hand"

    @staticmethod
    def DiscardResourceIconFromHand(player: 'Player', size: int, effect: 'Effect', *, color: List["Resources.RBYG"]=Resources.RBYG_LIST):
        from game.card.face.base import ClassCard
        from game.operate.faces import Faces
        from game.operate.faces_counter import FacesCounter
        from game.operate.filter import Filter

        icons = FacesCounter.GetPrintedResourcesCount(player.hand_cards.Get(), color, from_deck=player.hand_cards)
        if icons == 0:
            return
        elif icons < size:
            faces = Filter.ByType(player.hand_cards.Get(), ClassCard, CardFinder(has_printed_res=True))
            Faces.DiscardAll(faces, effect)
            return

        while True:
            discard_effect = player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    Players.DISCARD_RESOURCE_ICONS_PROMPT.format(size=size),
                    lambda targets:
                        Faces.DiscardAll(targets, effect)
                ).SetTarget(ClassCard,
                    from_where=["YourHandCards"],
                    combined_resource_icons=(size, size),
                    canbe_discard=True
                )
            )
            if discard_effect:
                return

    @staticmethod
    def DrawUp(targets: Sequence['CardFace'], size: int|Literal["Max"], by_effect: 'Effect'):
        Players.ForEachTargets(
            targets,
            lambda player:
                player.DrawUp(size, by_effect)
        )

    # class Addable(Protocol):
    #     def __add__(self, other: Any) -> 'Players.Addable':
    #         ...

    T = TypeVar("T")
    @staticmethod
    def ForEachPlayer(by_effect: 'Effect', action: Callable[['Player'], Any], result: T=None) -> T:
        from game.operate.worlds import Worlds
        return_result = result
        for player in Worlds.GetPlayers(by_effect):
            action_result = action(player)
            if return_result != None and action_result != None:
                return_result += action_result
            if by_effect.world.is_game_over:
                break
        return return_result # type: ignore

    @staticmethod
    def ForEachTargets(targets: Sequence['CardFace'], action: Callable[['Player'], Any]):
        for target in targets:
            player = target.GetControlBy()
            if Player.IsType(player):
                action(player)
                if player.world.is_game_over:
                    break

    ################################################################################
    # Discard "Hero Action", "Hero Response" Attachment

    # The label the attachment discard is offered under.
    #
    # Same rule as `SearchInternal.MAY_SEARCH_PROMPT` (MARVEL-112): an option
    # with no name cannot be answered by anyone, because `Effect.Render` falls
    # back to the *binding* effect's display name when a `ForChoiceAbility` has
    # none. Nameless, this clause was offered as "Play" -- the name of the event
    # being played -- so 49008 Electromagnetic Blast and 32038 Phase Strike asked
    # "Play?" and expected the player to know from elsewhere that yes discards an
    # attachment.
    #
    # Card-independent on purpose: all three callers print "discard an attachment
    # with the text "Hero Action" or "Hero Response"", so one label covers the
    # opt-in half (49008, 32038) and the forced half (26011 Phase Disruption)
    # alike. The attachment itself is named by the option's target list, so the
    # label states the action and nothing else.
    DISCARD_ATTACHMENT_PROMPT = "Discard an attachment"

    @staticmethod
    def DiscardHeroActionAttachment(player: 'Player', enemies: List['CardFace']|None, effect: 'Effect', *, may: bool):
        from game.operate.faces import Faces

        if enemies:
            attachments = [y for x in enemies for y in x.GetAttachedAttachments()]
            ability = AbilityFactory.ForChoiceAbility(
                Players.DISCARD_ATTACHMENT_PROMPT,
                lambda targets:
                    Faces.DiscardAll(targets, effect)
            ).SetTarget(attachments, with_texts=["Hero Action", "Hero Response"], canbe_discard=True)
        else:
            ability = AbilityFactory.ForChoiceAbility(
                Players.DISCARD_ATTACHMENT_PROMPT,
                lambda targets:
                    Faces.DiscardAll(targets, effect)
            ).SetTarget(Attachment, with_texts=["Hero Action", "Hero Response"], canbe_discard=True)

        if may:
            player.MayChooseOneAbility(
                effect,
                ability
            )
        else:
            player.ChooseAbilities(
                effect,
                ability
            )

    ################################################################################
    #
    @staticmethod
    def EachCharacterGetsUntilPhaseEnd(
        player: 'Player|None',
        by_effect: 'Effect',
        finder: 'CardFinder|None'=None,
        *,
        attack: int|None=None,
        defense: int|None=None,
        thwart: int|None=None,
    ):
        from game.operate.worlds import Worlds
        from game.operate.run_at import RunAt
        if player:
            characters = player.GetControlCharacters(finder)
        else:
            characters = Worlds.GetOnFieldCharacters(by_effect, finder)

        units: Dict['CardFace', int] = {}

        def process_fn(face: 'CardFace', diff: int):
            if face in units:
                if units[face] == diff:
                    return False
                units[face] += diff
            else:
                units[face] = diff

            if face.Gain(
                by_effect,
                diff,
                attack=attack,
                defense=defense,
                thwart=thwart,
            ):
                Message.AfterCardApply_Text([face], by_effect, diff)

        for unit in characters:
            process_fn(unit, +1)

        def action(effect: 'Effect', message: 'Message.AfterCardGainTrait|Message.AfterCardLoseTrait|Message.AfterCardLeavePlay|Message.AfterCardEnterPlay') -> None:
            if not finder or finder.Check(message.trigger):
                diff = +1
            else:
                diff = -1
            process_fn(message.trigger, diff)

        def check(effect: 'Effect', message: 'Message.AfterCardGainTrait|Message.AfterCardLoseTrait|Message.AfterCardLeavePlay|Message.AfterCardEnterPlay') -> bool:
            if player != None and message.trigger.GetControlBy() != player:
                if isinstance(message, Message.AfterCardLeavePlay):
                    found_units = [unit for unit in units if unit.card == message.trigger.card]
                    for unit in found_units:
                        units.pop(unit, None)
                return False

            if isinstance(message, Message.AfterCardLeavePlay):
                units.pop(message.trigger, None)
                # process_fn(message.trigger, -1)
            elif isinstance(message, Message.AfterCardEnterPlay):
                found_units = [unit for unit in units if unit.card == message.trigger.card and unit != message.trigger]
                for unit in found_units:
                    units.pop(unit, None)
                    # process_fn(unit, -1)

            if not finder or finder.Check(message.trigger):
                diff = +1
            else:
                diff = -1

            if message.trigger not in units:
                if diff == +1:
                    return True
                else:
                    return False
            elif units[message.trigger] != diff:
                return True

            return False

        by_effect.this.effect.RegisterTemp(
            Ability(
                AbilityType.Temp0,
                Message.AfterCardGainTrait|Message.AfterCardLoseTrait|Message.AfterCardLeavePlay|Message.AfterCardEnterPlay,
                [check],
                action,
            ),
            unregister_after_exec=False,
            until_phase_end=True
        )

        def action2():
            for unit in units:
                process_fn(unit, -1 * units[unit])

        RunAt.TheEndOfThePhase(by_effect, action2)

