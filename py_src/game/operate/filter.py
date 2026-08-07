from . import *

@final
class Filter:
    @staticmethod
    # Ties, AskSelectOne
    def One(original_faces: Sequence['TC'],
            by_effect: 'Effect',
            *,
            highest_cost: bool=False,
            lowest_cost: bool=False,
            fewest_remaining_hp: bool=False,
            most_damage: bool=False,
            least_damage: bool=False,
            engaged_with_fewest_minions: bool=False,
            most_remaining_hp: bool=False,
            highest_atk: bool=False,
            lowest_thw: bool=False,

            fewest_counter: 'CardFace.COUNTER|Literal[False]'=False,

            least_threat: bool=False,
            most_threat: bool=False,
            ) -> 'TC|None':
        if not original_faces:
            return None

        targets_faces = original_faces

        faces = Filter.All(
            targets_faces,
            type(targets_faces[0]),
            highest_cost=highest_cost,
            lowest_cost=lowest_cost,
            fewest_remaining_hp=fewest_remaining_hp,
            most_damage=most_damage,
            least_damage=least_damage,
            engaged_with_fewest_minions=engaged_with_fewest_minions,
            most_remaining_hp=most_remaining_hp,
            highest_atk=highest_atk,
            lowest_thw=lowest_thw,
            least_threat=least_threat,
            most_threat=most_threat,
            fewest_counter=fewest_counter,
        )

        if faces == []:
            return None
        elif len(faces) == 1:
            return faces[0]
        else:
            from game.effect.rule import Ties
            first_player = by_effect.world.GetFirstPlayer()
            return first_player.AskChooseFace(faces, Ties(world=by_effect.world), peek=True)

    ################################################################################
    #
    # See game\selector\selector_filter.py `FilterArgs`
    @staticmethod
    def All(original_faces: Sequence['CardFace'],
            card_type: Type['TC']|CardFace=CardFace,
            *,
            most_traits: bool=False,
            highest_cost: bool=False,
            lowest_cost: bool=False,
            most_printed_res: bool=False,
            engaged_with_fewest_minions: bool=False,
            most_remaining_hp: bool=False,
            fewest_remaining_hp: bool=False,
            most_damage: bool=False,
            least_damage: bool=False,
            highest_printed_hp: bool=False,
            highest_atk: bool=False,
            lowest_atk: bool=False,
            highest_printed_atk: bool=False,
            lowest_printed_atk: bool=False,
            highest_thw: bool=False,
            lowest_thw: bool=False,
            highest_sch: bool=False,
            lowest_sch: bool=False,
            least_threat: bool=False,
            most_threat: bool=False,
            lowest_order: bool=False,
            highest_order: bool=False,
            fewest_counter: 'CardFace.COUNTER|Literal[False]'=False,
            max_size: int|None=None,
            ) -> List['TC']:
        from cards.pack.sm.sinister_six import GetLowestOrder, GetHighestOrder # type: ignore
        from game.card.face.attribute.has_cost import HasCost

        faces = list(original_faces)

        def all_max(the_faces: Sequence['CardFace'], card_type: Type['TF'], key: Callable[['TF'], int], *, max: bool) -> Sequence['CardFace']:
            type_faces = [x for x in the_faces if isinstance(x, card_type)]
            if max:
                x = Math.AllMax(type_faces, key)
            else:
                x = Math.AllMin(type_faces, key)
            return x


        if highest_cost or lowest_cost:
            faces = [x for x in faces if HasCost.IsType(x) and x.has_printed_cost]
            faces = all_max(faces, HasCost, key=lambda x: x.printed_cost.val, max=highest_cost)

        if most_printed_res:
            faces = all_max(faces, ClassCard, lambda face: face.printed_resource.val, max=most_printed_res)

        if engaged_with_fewest_minions:
            faces = all_max(faces, CardFace, lambda face: face.GetControlByPlayer().engaged_minions.GetSize(), max=False)

        if most_remaining_hp or fewest_remaining_hp:
            faces = all_max(faces, CanHealth, lambda face: face.health, max=most_remaining_hp)

        if most_damage or least_damage:
            faces = all_max(faces, CanHealth, lambda face: face.sustained, max=most_damage)

        if highest_atk or lowest_atk:
            faces = all_max(faces, HasAttack, lambda face: face.attack, max=highest_atk)

        if highest_thw or lowest_thw:
            faces = all_max(faces, HasThwart, lambda face: face.thwart, max=highest_thw)

        if highest_sch or lowest_sch:
            faces = all_max(faces, HasScheme, lambda face: face.scheme, max=highest_sch)

        if highest_printed_hp:
            faces = all_max(faces, CanHealth, lambda face: face.printed_health, max=highest_printed_hp)

        if highest_printed_atk or lowest_printed_atk:
            faces = all_max(faces, HasAttack, lambda face: face.printed_attack, max=highest_printed_atk)

        if fewest_counter:
            faces = all_max(faces, CanPlaceCounter, key=lambda x: x.GetCounters(fewest_counter), max=False)

        if lowest_order:
            faces = list(GetLowestOrder(faces))
        elif highest_order:
            faces = list(GetHighestOrder(faces))

        if most_traits:
            faces = all_max(faces, CardFace, lambda face: len(face.traits), max=most_traits)

        if most_threat or least_threat:
            faces = all_max(faces, Scheme2, lambda face: face.threat, max=most_threat)

        if max_size == None:
            return faces # type: ignore
        else:
            return faces[:max_size] # type: ignore

    # Call `finder.Checks`
    @staticmethod
    def ByFinder(faces: Sequence['TC'],
                finder: 'CardFinder|None'=None,
                max: int=Math.INT_INF,
                ) -> List['TC']:
        if not finder:
            return list(faces)[:max]
        else:
            face_list: List['Any'] = []
            for face in faces:
                if finder.Check(face):
                    face_list.append(face)
                if len(face_list) >= max:
                    break
            return face_list

    @staticmethod
    # finders: or
    def ByType(faces: Sequence['CardFace'],
                #  card_type: 'Type['TC']'=CardFace,
                card_type: 'Type[TF]',
                *finders: 'CardFinder',
                return_rest_faces: List['CardFace']|None=None
                ) -> List['TF']:
        face_list: List[card_type] = []
        rest_faces: List['CardFace'] = []
        for face in faces:
            do_append = False
            if card_type.IsType(face):
                if finders:
                    for finder in finders:
                        if finder.Check(face):
                            do_append = True
                            break
                else:
                    do_append = True
                if do_append:
                    face_list.append(face)
            if not do_append:
                rest_faces.append(face)
        if return_rest_faces:
            return_rest_faces.clear()
            return_rest_faces.extend(rest_faces)
        return face_list

    ################################################################################
    # @staticmethod
    # def FindCard(faces: Sequence['TC'],
    #              finder: 'CardFinder|None'=None,
    #              *,
    #              name: str|None=None,
    #              trait: "CardFace.TRAITS|None"=None,
    #              card_class: "PlayerCard.CardClass|None"=None,
    #              card_type: Type['TC']|None=None,
    #              check_fn: Callable[['TC'], bool]|None=None # type: ignore
    #              ) -> TC|None:
    #     found_faces = Faces.FindCards(faces,
    #                                 finder=finder,
    #                                 name=name,
    #                                 trait=trait,
    #                                 card_class=card_class,
    #                                 card_type=card_type,
    #                                 check_fn=check_fn,
    #                                 max=1)
    #     return found_faces[0] if found_faces != [] else None

    # @staticmethod
    # def FindCards(faces: Sequence['TC'],
    #               finder: 'CardFinder|None'=None,
    #               *,
    #               name: str|None=None,
    #             #   subname: str|None=None,
    #               trait: "CardFace.TRAITS|None"=None,
    #               card_class: "PlayerCard.CardClass|None"=None,
    #               card_type: Type['TC']|None=None,
    #               game_area: 'GameArea|None'=None,
    #             #   canbe_tough: bool|None=None,
    #             #   canbe_confused: bool|None=None,
    #             #   canbe_stun: bool|None=None,
    #             #   canbe_exhaust: bool|None=None,
    #             #   is_tough: bool|None=None,
    #             #   is_confuse: bool|None=None,
    #             #   is_stun: bool|None=None,
    #             #   face_up: bool|None=None,
    #               check_fn: Callable[['TC'], bool]|None=None,
    #             #   set_name: "CardFace.SETNAMES|None"=None,
    #             #   has_counter: 'CardFace.COUNTER|None'=None,
    #             #   has_token: 'CardFace.TOKEN|None'=None,
    #               max: int=int_inf) -> List['TC']:
    #     if max == 0:
    #         return []
    #     from game.card.card_finder import CardFinder
    #     card_finder = CardFinder(name=name,
    #                             #  subname=subname,
    #                              trait=trait,
    #                              card_class=card_class,
    #                              card_type=card_type,
    #                              game_area=game_area,
    #                             #  canbe_tough=canbe_tough,
    #                             #  canbe_confused=canbe_confused,
    #                             #  canbe_stunned=canbe_stun,
    #                             #  canbe_exhaust=canbe_exhaust,
    #                             #  is_tough=is_tough,
    #                             #  is_confuse=is_confuse,
    #                             #  is_stun=is_stun,
    #                             #  face_up=face_up,
    #                              check_fn=check_fn,
    #                             #  has_counter=has_counter,
    #                             #  has_token=has_token,
    #                             #  set_name=set_name,
    #                              finder=finder)
    #     from typing import Any
    #     cards: List[Any] = [] # We should fix this late
    #     for face in faces:
    #         if card_finder.Check(face):
    #             # if not card_type or isinstance(face, card_type):
    #             cards.append(face)
    #             if len(cards) >= max:
    #                 break
    #     return cards

