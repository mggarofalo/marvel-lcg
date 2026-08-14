from . import *

@final
class SearchInternal:

    # The label the opt-in half of a `may` search is offered under.
    #
    # A `may` search is two decisions printed as one sentence -- "do you want to
    # search?" and "which card?" -- and the first one has to be a named option or
    # no player, human or bot, can answer it. Without a name `Effect.Render`
    # falls back to the *binding* effect's display name, so the opt-in reads as
    # "When_Defeated" and is indistinguishable from the trigger that caused it.
    #
    # It is deliberately card-independent: every card that reaches here prints
    # "may search their deck ... ", so one word covers all of them, and a spec
    # transcript that answers it stays readable.
    MAY_SEARCH_PROMPT = "Search"

    @staticmethod
    def SearchForCardsInternal(by_effect: 'Effect',
                                player: 'Player',
                                all_faces: Sequence['CardFace'],
                                process_choose: Callable[[TC], Any]|None,
                                process_other: Callable[['CardFace'], Any]|None,
                                finder: 'CardFinder|None',
                                may: bool,
                                not_move: bool=False, # will not shuffle
                                range: 'SELECT.RANGE_TYPE'=(1,1),
                                ) -> List[TC]:

        from game.card.face.card_face import CardFace

        legal_faces: List[CardFace] = []
        if finder:
            legal_faces = finder.Checks(all_faces)
        else:
            legal_faces = list(all_faces)

        # if by_find and not legal_faces:
        #     return []

        skip_choose = True
        if range == "All":
            skip_choose = True
        else:
            check_faces_deck: Dict[str, Deck] = {}
            for face in legal_faces:
                face_name = face.name
                if face_name in check_faces_deck:
                    if check_faces_deck[face_name] != face.card.area:
                        skip_choose = False
                        break
                else:
                    check_faces_deck[face_name] = face.card.area
                    if len(check_faces_deck.keys()) > 1:
                        skip_choose = False
                        break

        if skip_choose and legal_faces != []:
            select_face = legal_faces
        else:
            select_face = all_faces

        return_face: List[Any] = []

        # `may` used to widen the range to (0, max) here. That is the wrong
        # shape for an opt-in and it made every "may search" card a no-op:
        #
        #   * The minimum became 0, and picking the minimum is what every
        #     automated player does -- `BotCommand.Build` takes
        #     `all_legal_targets[:target_num_range[0]]`, which is the same
        #     minimum `PlayerAction` assigns to a forced effect. So the search
        #     always came back empty.
        #   * The decision still went out `forced=True`, so the client showed no
        #     cancel button and the one option it did show carried no name. The
        #     opt-in was not expressible: there was no "no" to choose and no "yes"
        #     to read.
        #
        # "May" is a choice between two abilities, not a target count. Below,
        # the range is left alone -- a search that happens finds a card -- and
        # declining is offered as its own option by `MayChooseOneAbility`, via
        # `AskChooseSelect(forced=False)`. That is the shape 51026 Build Support
        # already spelled out by hand, and MARVEL-106 established it as the one
        # that works.
        #
        # A consequence worth stating: with the range back at (1, max), a search
        # that can find nothing has no legal targets, `Selector.GetTargetRange`
        # returns None, and the effect is filtered out before the prompt. The
        # player is asked only when there is something to say yes to.

        # We use this to do shuffle
        selector = Select.From(faces=select_face,
                                finder=finder,
                                not_move=not_move,
                                not_shuffle=False,
                                by_search=True,
                                range=range)
        if skip_choose and not may:
            faces = selector.GetAllLegalTargets(by_effect)
            min = selector.selector_range.GetTargetMin(by_effect, faces)
            max = selector.selector_range.GetTargetMax(by_effect, faces)
            faces = faces[:max]
            if not selector.AfterSelectTargets(by_effect, faces, (min, max)):
                # selector.peek = True
                selector.IfSelectTargetFailure(by_effect)
                faces = []
        elif may:
            faces = player.AskChooseSelect(
                selector,
                by_effect,
                prompt=SearchInternal.MAY_SEARCH_PROMPT,
                forced=False,
            )
        else:
            faces = player.AskChooseSelect(selector, by_effect)

        def process(targets: Sequence['CardFace']) -> None:
            for face in targets:
                if process_choose:
                    process_choose(face) # type: ignore
                nonlocal return_face
                return_face.append(face)

            if process_other:
                for face in all_faces:
                    if face not in targets:
                        process_other(face)
        if faces:
            process(faces)

        return return_face

    ################################################################################
    #
    @staticmethod
    def FindCards(by_effect: 'Effect',
                *,
                range: 'SELECT.RANGE_TYPE'=(1,1),
                finder: 'CardFinder|None'=None,
                name: str|None=None,
                trait: "CardFace.TRAITS|None"=None,
                card_type: Type['TC']|CardFace=CardFace,
                include_in_play: bool=True,
                **kwargs: Unpack['CardFinder.KWArgs'],
                ) -> List['CardFace']:
        from game.operate.search import Search

        player = by_effect.world.GetFirstPlayer()
        faces = Search.SearchForCards(
            by_effect,
            player,
            include_encounter_deck=True,
            include_encounter_discard_pile=True,
            include_set_aside=True,
            include_in_play=include_in_play,
            # include_victory_display=True,
            finder=CardFinder(
                name=name,
                trait=trait,
                card_type=card_type,
                **kwargs) & finder,
            range=range,
            # by_find=True,
        )
        return faces

