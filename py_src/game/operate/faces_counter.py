from . import *

@final
class FacesCounter:
    ################################################################################
    #
    # each different card type
    @staticmethod
    def GetDifferentTypesCount(faces: Sequence['CardFace']) -> int:
        types: Dict[Type[Any], int] = {}
        count = 0
        for face in faces:
            the_type = type(face)
            if the_type in types:
                pass
            else:
                types[the_type] = 1
                count += 1
        return count

    # each different card *title*. "Cards with the same title are considered to
    # be the same card for the purpose of card abilities" -- so two copies of
    # one title count once, and this is deliberately not `len(set(faces))`.
    # Matches `Cards.getCard(id).name` in `public/js/marvel/effect.ts`, which is
    # `CardDescriptor.name`, which is `face.name`.
    @staticmethod
    def GetDifferentCardsCount(faces: Sequence['CardFace']) -> int:
        names: Dict[str, int] = {}
        count = 0
        for face in faces:
            if face.name in names:
                pass
            else:
                names[face.name] = 1
                count += 1
        return count

    @staticmethod
    def GetAspectCount(faces: Sequence['CardFace']) -> int:
        from game.card.face.base import ClassCard
        classes: Dict[Any, int] = {}
        count = 0
        for face in faces:
            if ClassCard.IsType(face):
                if "Aspect" in face.card_classes:
                    for check_class in face.card_classes:
                        if check_class == "Aspect":
                            continue
                        if check_class in classes:
                            pass
                        else:
                            classes[check_class] = 1
                            count += 1
        return count

    @staticmethod
    # ["YY", "B"] -> "YYB"
    def GetPrintedResources(faces: Sequence['CardFace']) -> 'Resources':
        from game.element.resources import Resources
        from game.card.face.base import ClassCard
        res = Resources("0")
        for face in faces:
            if ClassCard.IsType(face):
                res += face.printed_resource
        return res

    @staticmethod
    # ["YY", "B", "B"] -> 2
    # ["GG", "B", "B"] -> 2
    # each different resource icon
    def GetPrintedResourcesTypes(faces: Sequence['CardFace']) -> int:
        return FacesCounter.GetPrintedResources(faces).GetResourceIconTypes()

    @staticmethod
    # ["YY", "B"] -> {"Y": 2, "B": 1}
    def GetPrintedResourcesCountInternal(faces: Sequence['CardFace'], check_from_deck: 'Deck|Literal["Auto"]'="Auto") -> Dict["Resources.RBYG", int]:
        from game.card.face.base import ClassCard
        from game.message import Message
        return_res: Dict["Resources.RBYG", int] = {
            "R": 0,
            "B": 0,
            "Y": 0,
            "G": 0,
        }
        for face in faces:
            if ClassCard.IsType(face):
                if check_from_deck == "Auto":
                    from_deck = face.card.area
                else:
                    from_deck = check_from_deck
                message = Message.WhenCountingResourcesOnCards(face, from_deck)
                message.Send()
                res = message.return_res
                for color in Resources.RBYG_LIST:
                    value = res.GetColor(color, convert_green_res=False)
                    return_res[color] += value
        return return_res

    @staticmethod
    # ["YY", "B"], ["Y"] -> 2
    def GetPrintedResourcesCount(faces: Sequence['CardFace'], colors: List["Resources.RBYG"]|"Resources.RBYG", from_deck: 'Deck|Literal["Auto"]'="Auto") -> int:
        if not isinstance(colors, list):
            colors = [colors]
        res_dict = FacesCounter.GetPrintedResourcesCountInternal(faces, from_deck)
        value = 0
        for color in colors:
            value += res_dict[color]
        return value

    @staticmethod
    # ["YY", "B"] -> 3 (.val)
    def GetPrintedResourcesIcon(faces: Sequence['CardFace'], from_deck: 'Deck|Literal["Auto"]') -> int:
        return FacesCounter.GetPrintedResourcesCount(faces, Resources.RBYG_LIST, from_deck)

    ################################################################################
    #
    @staticmethod
    def GetPrintedResourceCost(faces: Sequence['CardFace']) -> int:
        return FacesCounter.GetPrintedCost(faces)

    @staticmethod
    def GetTotalCost(faces: Sequence['CardFace']) -> int:
        return FacesCounter.GetPrintedCost(faces)

    @staticmethod
    def GetPrintedCost(faces: Sequence['CardFace']) -> int:
        from game.card.face.attribute.has_cost import HasCost
        total_res = 0
        for face in faces:
            if HasCost.IsType(face):
                total_res += face.printed_cost.val
        return total_res

    ################################################################################
    #
    @staticmethod
    def GetMostCommonType(faces: Sequence['CardFace']) -> int:
        return FacesCounter.GetMaxSameTypeCount(faces)

    @staticmethod
    def GetMaxSameTypeCount(faces: Sequence['CardFace']) -> int:
        type_counts = collections.Counter(type(item) for item in faces)
        # Return the maximum count
        return max(type_counts.values(), default=0)

    ################################################################################
    #
    @staticmethod
    def GetSetupPermanentFaces(faces: Sequence['CardFace']) -> Sequence['CardFace']:
        return [
            x for x in faces
            if HasSetup.IsType(x) and x.printed_setup or
            HasPermanent.IsType(x) and x.printed_permanent
        ]

    ################################################################################
    #
    @staticmethod
    def CountTotalBoostIcons(faces: Sequence['TC']) -> int:
        from game.card.face.attribute.has_boost_icon import HasBoostIcon
        value = 0
        for face in faces:
            if HasBoostIcon.IsType(face):
                value += face.CountBoostIconsInternal()
        return value

    @staticmethod
    def CountTotalBoostStar(faces: Sequence['TC']) -> int:
        from game.card.face.attribute.has_boost_icon import HasBoostIcon
        value = 0
        for face in faces:
            if HasBoostIcon.IsType(face):
                value += face.boost_star
        return value

    @staticmethod
    def CountTotalBoostStarAndBoost(faces: Sequence['TC']) -> int:
        from game.card.face.attribute.has_boost_icon import HasBoostIcon
        value = 0
        for face in faces:
            if HasBoostIcon.IsType(face):
                value += face.boost_star
                value += face.CountBoostIconsInternal()
        return value

    # @staticmethod
    # def CountTotalResourcesIcons(faces: Sequence['TC'], res: 'Resources.RBYG|None'=None) -> int:
    #     from game.card.face import PlayerCard
    #     value = 0
    #     for face in faces:
    #         if PlayerCard.IsType(face):
    #             if res != None:
    #                 value += face.printed_resource.GetColor(res, convert_green_res=False)
    #             else:
    #                 value += face.printed_resource.val
    #     return value