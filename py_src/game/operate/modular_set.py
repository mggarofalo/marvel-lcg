from . import *

class AsideModularSet:
    ################################################################################
    # aside modular
    @staticmethod
    def ChooseRandom(effect: 'Effect', do_shuffle: bool=False) -> List['CardFace']:
        from game.operate.faces import Faces
        from game.operate.faces_counter import FacesCounter
        from game.operate.worlds import Worlds
        from game.operate.rand import Rand

        modular_sets: Dict[str, List['CardFace']] = {}
        for face in Worlds.AsideModular(effect).Get():
            set_name = face.paper.set_name
            if set_name not in modular_sets:
                modular_sets[set_name] = []
            modular_sets[set_name].append(face)

        names = [x for x in modular_sets]

        select = Rand.RandomChoice(names, effect)

        if do_shuffle:
            setup_faces = FacesCounter.GetSetupPermanentFaces(modular_sets[select])
            faces = [x for x in modular_sets[select] if x not in setup_faces]
            Faces.ShuffleAllTo(faces, "EncounterDeck", effect)
            # Shuffle first for "21129"
            for face in setup_faces:
                face.PutIntoPlay("CurrentPlayer", effect)
                # ~~Call reveal for "45127" "45133" "45139"~~
                # face.Reveal("FirstPlayer", effect)

        return modular_sets[select]

    @staticmethod
    def GetSize(effect: 'Effect') -> int:
        from game.operate.worlds import Worlds
        modular_sets: List[str] = []
        for face in Worlds.AsideModular(effect).Get():
            set_name = face.paper.set_name
            if set_name not in modular_sets:
                modular_sets.append(set_name)
        return len(modular_sets)

