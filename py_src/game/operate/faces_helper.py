from . import *

@final
class FacesHelper:

    @staticmethod
    def ListToDict(targets: Sequence['TC']) -> Dict['TC', int]:
        faces: Dict[Any, int] = {}
        for target in targets:
            if target in faces:
                continue
            else:
                faces[target] = targets.count(target)
        return faces

    ################################################################################
    #
    @staticmethod
    def AssertCardsAreDifferent(faces: Sequence['CardFace']) -> int:
        names: List[str] = []
        for face in faces:
            if face.name in names:
                assert False
        return True

