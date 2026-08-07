from . import *

@final
class SearchInternal:

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

        if may:
            # Hack
            assert isinstance(range, Tuple)
            x = range[1]
            assert not isinstance(x, str)
            range = (0, x)

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

