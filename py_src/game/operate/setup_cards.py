from . import *

class SetupCards:

    @staticmethod
    def PutIntoPlay(by_effect: 'Effect',
                    for_player: 'Player|Literal["FirstPlayer"]'="FirstPlayer",
                    under_control: bool=False,
                    *,
                    flip_to_trait: 'CardFace.TRAITS|None'=None,
                    flip_to_name: str|None=None,
                    finder: 'CardFinder|None'=None,
                    name: str|None=None,
                    trait: "CardFace.TRAITS|None"=None,
                    card_type: Type['TC']|CardFace=CardFace,
                    from_where: List[Literal["SetAside"]]=[], # TODO: Add this
                    **kwargs: Unpack['CardFinder.KWArgs'],
                    ) -> 'TC|None':
        faces = SearchInternal.FindCards(
            by_effect,
            finder=finder,
            name=name,
            trait=trait,
            card_type=card_type,
            include_in_play=False,
            **kwargs)
        put_faces: List['Any'] = []
        for face in faces:
            if flip_to_trait or flip_to_name:
                face.FlipTo(by_effect, trait=flip_to_trait, name=flip_to_name)
            this = face.card.face
            if this.PutIntoPlay(for_player, by_effect, under_control=under_control):
                put_faces.append(this)
        if put_faces:
            return put_faces[0]
        else:
            return None

    @staticmethod
    def Reveal(by_effect: 'Effect',
                reveal: 'Player|Literal[True]'=True,
                *,
                range: 'SELECT.RANGE_TYPE'=(1,1),
                finder: 'CardFinder|None'=None,
                name: str|None=None,
                trait: "CardFace.TRAITS|None"=None,
                card_type: Type['TC']|CardFace=CardFace,
                only_set_aside: bool=False,
                **kwargs: Unpack['CardFinder.KWArgs'],
                ) -> 'CardFace|None':
        from game.operate.worlds import Worlds
        if only_set_aside:
            faces = Worlds.GetSetAsideAreaCards(
                by_effect,
                CardFinder(
                    card_type=card_type,
                    name=name,
                    trait=trait,
                    **kwargs) & finder
            )
        else:
            faces = SearchInternal.FindCards(
                by_effect,
                range=range,
                finder=finder,
                name=name,
                trait=trait,
                card_type=card_type,
                **kwargs)
        for face in faces:
            if isinstance(reveal, Player):
                face.Reveal(reveal, by_effect)
            else:
                face.Reveal(None, by_effect)
        if faces:
            return faces[0]
        else:
            return None

    @staticmethod
    def ShuffleIntoEncounterDeck(by_effect: 'Effect',
                                finder: 'CardFinder|None'=None,
                                name: str|None=None,
                                trait: "CardFace.TRAITS|None"=None,
                                card_type: Type['TC']|CardFace=CardFace,
                                **kwargs: Unpack['CardFinder.KWArgs'],
                                ):
        faces = SearchInternal.FindCards(
            by_effect,
            finder=finder,
            name=name,
            trait=trait,
            card_type=card_type,
            **kwargs)
        for face in faces:
            Faces.ShuffleAllTo([face], "EncounterDeck", by_effect)

    @staticmethod
    def AttachTo(by_effect: 'Effect',
                 attach_to: 'CardFace',
                 *,
                 flip_to_trait: 'CardFace.TRAITS|None'=None,
                 flip_to_name: str|None=None,
                 finder: 'CardFinder|None'=None,
                 name: str|None=None,
                 trait: "CardFace.TRAITS|None"=None,
                 card_type: Type['TC']|CardFace=CardFace,
                 choose: Literal["First", "Random", "Ask"]="First",
                 include_in_play: bool=True,
                 **kwargs: Unpack['CardFinder.KWArgs'],
                 ):
        range = (1,1) if choose == "First" else "All"
        faces = SearchInternal.FindCards(
            by_effect,
            range=range,
            finder=finder,
            name=name,
            trait=trait,
            card_type=card_type,
            include_in_play=include_in_play,
            **kwargs)

        if choose == "Random":
            face = Random.RandomChoice(faces)
        elif choose == "Ask":
            face = by_effect.world.GetFirstPlayer().AskChooseFace(faces, by_effect)
        else:
            face = faces[0] if faces else None

        if CanAttach.IsType(face):
            if flip_to_trait or flip_to_name:
                face.FlipTo(by_effect, trait=flip_to_trait, name=flip_to_name)
            face.AttachTo2(attach_to, by_effect)

    @staticmethod
    def DealAsFacedownEncounterCard(by_effect: 'Effect',
                                    to_player: 'Player',
                                    finder: 'CardFinder|None'=None,
                                    name: str|None=None,
                                    trait: "CardFace.TRAITS|None"=None,
                                    card_type: Type['TC']|CardFace=CardFace,
                                    **kwargs: Unpack['CardFinder.KWArgs'],
                                    ):
        faces = SearchInternal.FindCards(
            by_effect,
            finder=finder,
            name=name,
            trait=trait,
            card_type=card_type,
            **kwargs)
        for face in faces:
            to_player.DealEncounterCard(face, by_effect)

    @staticmethod
    def SetAsideCards(by_effect: 'Effect',
                    *,
                    finder: 'CardFinder|None'=None,
                    name: str|None=None,
                    trait: "CardFace.TRAITS|None"=None,
                    card_type: Type['TC']|CardFace=CardFace,
                    **kwargs: Unpack['CardFinder.KWArgs'],
                    ) -> List['CardFace']:
        from game.operate.worlds import Worlds

        deck = Worlds.GetEncounterDeck(by_effect)
        faces: List['CardFace'] = []
        faces += Worlds.FindCardsOnField(
            by_effect,
            finder,
            name=name,
            trait=trait,
            card_type=card_type,
            **kwargs)
        faces += deck.FindCards(
            finder=finder,
            name=name,
            trait=trait,
            card_type=card_type,
            **kwargs)

        Faces.SetAside(faces, by_effect)
        return faces

