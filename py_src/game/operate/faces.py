from . import *

@final
class Faces:
    ################################################################################
    #
    @staticmethod
    def DiscardAll(faces: Sequence['CardFace'], by_effect: 'Effect', *, into_area: 'Literal["Owner"]|Deck|None'=None, ignore_identity: bool=False, simultaneous: bool=False) -> List['CardFace']:
        from game.message import Message
        moved_faces: List['CardFace'] = []
        from_decks = Faces.GetDecksInDeck(faces)

        if simultaneous and faces:
            faces = by_effect.world.GetFirstPlayer().ChooseOrder(faces, prompt="Simultaneous Discard")

        for face in faces:
            if face.DiscardInternal(by_effect, into_area=into_area, ui_group=True, ignore_identity=ignore_identity):
                moved_faces.append(face)
        if moved_faces:
            Message.AfterCardsDiscard_Text({x: from_decks[x] for x in from_decks if x in moved_faces})
        return moved_faces

    @staticmethod
    # Return how many cards exhaust this way
    def ExhaustAll(faces: Sequence['CardFace'], by_effect: 'Effect') -> List['CardFace']:
        from game.message import Message
        processed_faces: List['CardFace'] = []
        for face in faces:
            if face.ExhaustInternal(by_effect, ui_group=True):
                processed_faces.append(face)
        if processed_faces:
            Message.AfterCardsExhaust_Text(processed_faces, by_effect)
        return processed_faces

    @staticmethod
    def ReadyAll(faces: Sequence['CardFace'], by_effect: 'Effect') -> List['CardFace']:
        from game.message import Message
        processed_faces: List[CardFace] = []
        for face in faces:
            if face.ReadyInternal(by_effect, ui_group=True):
                processed_faces.append(face)
        if processed_faces:
            Message.AfterCardsReadied_Text(processed_faces, by_effect)
        return processed_faces

    # TODO: remove `ui_group`
    @staticmethod
    def ShuffleAllTo(faces: Sequence['CardFace'], deck: 'Deck|Literal["EncounterDeck"]', by_effect: 'Effect', ui_group: bool=True) -> List['CardFace']:
        from game.message import Message
        from game.operate.worlds import Worlds
        if deck == "EncounterDeck":
            deck = Worlds.GetEncounterDeck(by_effect)
        Message.CardsShuffleToDeck_Text(faces, deck)
        moved_faces = Faces.MoveAllTo(faces, deck, by_effect, by_shuffle=True)
        if moved_faces:
            deck.Shuffle(by_effect)
        return moved_faces

    @staticmethod
    def MoveAllToDeck(faces: Sequence['CardFace'], deck: 'Deck',
                position: Literal["Top", "Bottom"],
                by_effect: 'Effect',
                ) -> List['CardFace']:
        from game.message import Message
        moved_faces: List['CardFace'] = []
        from_decks = Faces.GetDecksInDeck(faces)
        for face in faces:
            if position == "Top":
                if face.card.MoveToTop(deck, by_effect, ui_group=True):
                    moved_faces.append(face)
            elif position == "Bottom":
                if face.card.MoveToBottom(deck, by_effect, ui_group=True):
                    moved_faces.append(face)
        if moved_faces:
            message = Message.AfterCardsMoved(
                {x: from_decks[x] for x in from_decks if x in moved_faces},
                move_by_position=True
            )
            message.Send()
        return moved_faces

    @staticmethod
    def MoveAllTo(faces: Sequence['CardFace'], area: 'Deck', by_effect: 'Effect',
                *,
                by_shuffle: bool=False,
                target_game_area: 'GameArea|None'=None,
                ) -> List['CardFace']:
        from game.message import Message
        moved_faces: List['CardFace'] = []
        from_areas = Faces.GetDecksInDeck(faces)
        for face in faces:
            if face.card.MoveToArea(area, by_effect, ui_group=True, target_game_area=target_game_area):
                moved_faces.append(face)
        if moved_faces:
            message = Message.AfterCardsMoved(
                {x: from_areas[x] for x in from_areas if x in moved_faces},
                by_shuffle=by_shuffle,
                by_discard=area.flags.is_discards,
                by_put_into_play=area.flags.is_in_play,
                by_place_on_table=area.flags.is_processing,
                by_return_to_hand=area.flags.is_in_hand,
                by_setaside=area.flags.is_set_aside,
                by_draw_up=area.flags.is_in_hand,
                by_resources=area.flags.is_resources_area,
                by_deal_encounter_card=area.flags.is_dealt_encounter,
                by_boost=area.flags.is_boost_area,
                by_remove=area.flags.is_removed,
            )
            message.Send()
        return moved_faces

    @staticmethod
    def MoveAllToProcessingArea(faces: Sequence['CardFace'], by_effect: 'Effect') -> List['CardFace']:
        from game.message import Message
        if not faces:
            return []

        moved_faces: List['CardFace'] = []
        face = faces[0]
        player = face.card.area.GetOwner()
        world = by_effect.world
        if isinstance(player, Player):
            area = player.area_processing
        else:
            area = world.area_processing

        moved_faces = Faces.MoveAllTo(faces, area, by_effect)
        # area = faces[0].card
        # from_decks = Faces.GetDecksInDeck(faces)
        # for face in faces:
        #     if face.card.MoveToProcessingAreaInternal(by_effect, ui_group=True):
        #         moved_faces.append(face)
        Message.AfterCardsPlaceOnTable_Text(moved_faces)
        return moved_faces

    @staticmethod
    def RemoveAllFromGame(faces: Sequence['TC'], by_effect: 'Effect') -> List['CardFace']:
        from game.message import Message
        moved_faces = Faces.MoveAllTo(faces, by_effect.world.area_removed, by_effect)
        Message.AfterCardsRemove_Text(moved_faces)
        return moved_faces

    @staticmethod
    def FlipAllTo(faces: Sequence['TC'], face_up: bool|None, by_effect: 'Effect') -> List['CardFace']:
        from game.message import Message
        flipped_faces: List['CardFace'] = []
        for face in faces:
            if face_up == None:
                face.card.Flip(by_effect, ui_group=True)
                flipped_faces.append(face)
            else:
                if face.FlipTo(by_effect, face_up=face_up, ui_group=True):
                    flipped_faces.append(face)
        Message.AfterCardsFlipAll_Text(flipped_faces)
        return flipped_faces

    @staticmethod
    def SetAside(faces: Sequence['TC'], by_effect: 'Effect') -> List['CardFace']:
        from game.message import Message
        moved_faces: List['CardFace'] = []
        from_decks = Faces.GetDecksInDeck(faces)
        for face in faces:
            if face.SetAsideInternal(by_effect, ui_group=True):
                moved_faces.append(face)
        Message.AfterCardsSetAside_Text({x: from_decks[x] for x in from_decks if x in moved_faces})
        return moved_faces

    @staticmethod
    def GiveStatus(units: Sequence['CardFace']|Literal["YourLeader", "EnemyLeader"], status: "CardFace.STATUS", by_effect: 'Effect') -> int:
        from game.card.face.base import Unit2
        from game.operate.worlds import Worlds
        if units == "YourLeader":
            return 0
        if units == "EnemyLeader":
            leader = Worlds.GetEnemyLeader(by_effect)
            if leader:
                units = [leader]
            else:
                return 0
        value = 0
        for unit in units:
            if unit.card.IsOnField():
                unit = unit.card.CastTo(Unit2)
                if unit.GainStatus(status, by_effect):
                    value += 1
        return value

    @staticmethod
    def HasStatus(units: Sequence['TC'], status: "CardFace.STATUS") -> int:
        from game.card.face.base import Unit2
        value = 0
        for unit in units:
            if unit.card.IsOnField():
                unit = unit.card.CastTo(Unit2)
                if unit.HasStatus(status):
                    value += 1
        return value

    @staticmethod
    def DefeatUnits(units: Sequence['CardFace'], killer: 'CardFace|None', by_effect: 'Effect', *, ignore_when_defeated: bool=False):
        for unit in units:
            if Unit2.IsType(unit):
                unit.Death(killer, by_effect, ignore_when_defeated=ignore_when_defeated)

    @staticmethod
    def PlaceCountersOn(targets: Sequence['TC'], value: int|Literal["1*", "2*", "3*", "6*"], name: 'CardFace.COUNTER|Literal["all-purpose"]', by_effect: 'Effect', *, maximum: int|None=None, by_player: 'Player|None'=None) -> int|None:
        from game.card.face import CanPlaceCounter
        total = 0
        for target in targets:
            target = target.card.CastTo(CanPlaceCounter)
            total += target.PlaceCountersInternal(value, name, by_effect, maximum=maximum)
        return total

    @staticmethod
    def RemoveCountersOn(targets: Sequence['TC'], value: int|Literal["All"], name: 'CardFace.COUNTER|Literal["all-purpose"]', by_effect: 'Effect', *, by_player: 'Player|None'=None) -> int|None:
        from game.card.face import CanPlaceCounter
        total = 0
        for target in targets:
            target = target.card.CastTo(CanPlaceCounter)
            message = target.RemoveCountersInternal(value, name, by_effect, forced=True)
            if message:
                total += message.removed_counters
        return total

    @staticmethod
    def PlaceTokensOn(targets: Sequence['TC'], value: int|Literal["2*"], name: 'CardFace.TOKEN', by_effect: 'Effect') -> int|None:
        from game.card.face import CanPlaceToken
        total = 0
        for target in targets:
            target = target.card.CastTo(CanPlaceToken)
            token_name = name
            size = target.PlaceTokensInternal(value, token_name, by_effect)
            if size:
                total += size
        return total

    @staticmethod
    def RemoveTokensOn(targets: Sequence['TC'], value: int|Literal["All", "1*"], name: 'CardFace.TOKEN', by_effect: 'Effect') -> int|None:
        from game.card.face import CanPlaceToken
        total = 0
        for target in targets:
            target = target.card.CastTo(CanPlaceToken)
            token_name = name
            size = target.RemoveTokensInternal(value, token_name, by_effect, forced=True)
            if size:
                total += size
        return total
    ################################################################################
    #
    @staticmethod
    def FindCardSize(faces: Sequence['CardFace'],
                    finder: 'CardFinder|None'=None,
                    ) -> int:
        if finder:
            return len(finder.Checks(faces))
        return len(faces)

    @staticmethod
    def FindCardSizeOld(faces: Sequence['CardFace'], finder: 'CardFinder') -> int:
        return len(finder.Checks(faces))

    @staticmethod
    def IsAll(faces: Sequence['CardFace'], finder: 'CardFinder') -> bool:
        for face in faces:
            if not finder.Check(face):
                return False
        return True

    ################################################################################
    #
    @staticmethod
    def EachOfAre(faces: Sequence['TC'], finder: 'CardFinder') -> bool:
        for face in faces:
            if not finder.Check(face):
                return False
        return True

    @staticmethod
    def GetDecksInDeck(faces: Sequence['CardFace']) -> Dict['CardFace', 'Deck']:
        # decks: List['Deck'] = []
        # for face in faces:
        #     deck = face.card.area
        #     if deck.flags.is_deck:
        #         decks.append(deck)
        # return decks
        # return list(set([x.card.area for x in faces]))
        return {x: x.card.area for x in faces}

    ################################################################################
    #
    # Call `FilterByFinder2`
    # @staticmethod
    # def GetHighestPrintedCost(faces: Sequence['CardFace']) -> List[HasCost]:
    #     cost = 0
    #     found_faces: List['HasCost'] = []
    #     for face in faces:
    #         if HasCost.IsType(face):
    #             if face.printed_cost.val > cost:
    #                 found_faces = []
    #                 cost = face.printed_cost.val
    #             if face.printed_cost.val == cost:
    #                 found_faces.append(face)
    #     return found_faces

    @staticmethod
    def ReturnToHand(faces: Sequence['CardFace'], owner: 'Player|Literal["Owner"]', by_effect: 'Effect') -> List['CardFace']:
        from game.message import Message
        returned_faces: List['CardFace'] = []
        player_cards: Dict[Player|None, List[CardFace]] = {None: []}

        for face in faces:
            if owner == "Owner":
                player = None
                if face.GetOwner().IsPlayer():
                    player = face.GetOwnerPlayer()
            else:
                player = owner

            if player == None:
                player_cards[None].append(face)
            else:
                if player not in player_cards:
                    player_cards[player] = []
                player_cards[player].append(face)

        Faces.DiscardAll(player_cards[None], by_effect)
        for player in player_cards:
            if player != None:
                player.GainCard(player_cards[player], by_effect)
                returned_faces += player_cards[player]

        Message.AfterCardsReturnToHand_Text(returned_faces, by_effect)
        return returned_faces

    @staticmethod
    def AddToHand(faces: Sequence['CardFace'], player: 'Player', by_effect: 'Effect') -> List['CardFace']:
        return Faces.ReturnToHand(faces, player, by_effect)

    @staticmethod
    def TreatAsAlly(minion: 'CardFace', as_card_name: str, player: 'Player', effect: 'Effect') -> bool:
        # from game.operate.worlds import Worlds
        from game.ability.factory import AbilityFactory
        from game.card.factory import CardFactory
        from game.operate.effects import Effects

        this = effect.this
        minion = minion.card.CastTo(Minion)

        paper = CardFactory.FindCardPapers(as_card_name)[0]
        face = CardFactory.CreateFace(paper, effect.world)

        if face and Ally.IsType(face):

            face.pic_id = minion.pic_id
            face.SetName(minion.printed_name)
            face.SetSubtitle(minion.printed_subtitle)

            face.printed_attack = minion.printed_attack
            face.printed_thwart = minion.printed_scheme

            minion.card.SwapCardFace(face, effect)
            CardFactory.FaceRegisterEffects(face)

            # face.GainAttack(minion.printed_attack, effect)
            # face.GainThwart(minion.printed_scheme, effect)

            minion.ResetKeywords()

            # face.GainAttackConsequentialDamage(2, effect)
            # face.GainThwartConsequentialDamage(2, effect)

            effects: List['Effect'] = []

            def action(action_effect: 'Effect', action_message: 'Message2'):
                # minion.PutIntoPlay(player, effect)
                Effects.UnRegister(effects)
                face.card.SwapCardFace(minion, action_effect)
                if minion.IsInPlay() and not minion.IsDefeated(): # Fix for "34009"
                    minion.PutIntoPlay(player, effect)
                minion.card.SetOwner(None)

            effects = [
                *face.effect.RegisterTemp(
                    AbilityFactory.AfterCardLeavePlay(
                        AbilityType.Temp2,
                        "This",
                        action,
                        # conditions=[condition]
                    ),
                    unregister_after_exec=False
                ),
                *face.effect.RegisterTemp(
                    AbilityFactory.WhenCardLeavePlay(
                        AbilityType.Temp2,
                        this,
                        action,
                        conditions=[
                            lambda effect, message:
                                not face.IsDefeated()
                        ]
                    ),
                    unregister_after_exec=False
                )
            ]

            face.PutIntoPlay(player, effect, under_control=True)
            return True
        return False

    @staticmethod
    def SwapWithDeckTop(faces: Sequence['CardFace'], deck: 'Deck', by_effect: 'Effect'):

        if faces:
            size = len(faces)
            top_faces = deck.GetTops(size)

            if top_faces:
                original_decks = [x.card.area for x in faces]
                Faces.MoveAllTo(faces, deck, by_effect)
                for i, face in enumerate(top_faces):
                    Faces.MoveAllTo([face], original_decks[i], by_effect)

    @staticmethod
    def GetRest(all_faces: Sequence['CardFace'], checked_faces: Sequence['CardFace']) -> List['CardFace']:
        faces = list(all_faces)
        for face in checked_faces:
            faces.remove(face)
        return faces

    @staticmethod
    def BindDiscardPile(faces: Sequence['CardFace'], discard_pile: 'Deck'):
        for face in faces:
            face.card.bind_discard_pile = discard_pile

    @staticmethod
    def LookAt(faces: Sequence['CardFace'], player: 'Player', by_effect: 'Effect'):
        from game.message import Message
        # reversed_face = faces[::-1]
        Message.LookAt_Text(player, faces)
        return faces

    @staticmethod
    def AddToVictoryDisplay(faces: Sequence['CardFace'], by_effect: 'Effect'):
        return Faces.MoveAllTo(faces, by_effect.world.victory_display, by_effect)

    @staticmethod
    def GiveFacedownBoostCards(faces: Sequence['CardFace'], num: int, by_effect: 'Effect') -> None:
        from game.card.face.attribute.can_boost import CanBoost
        for face in faces:
            if CanBoost.IsType(face):
                face.GiveFacedownBoostCardsInternal(num, by_effect, None)

    @staticmethod
    def GetTotalCounters(faces: Sequence['CardFace'], counter: 'CardFace.COUNTER') -> int:
        size = 0
        for face in faces:
            if CanPlaceCounter.IsType(face):
                size += face.GetCounters(counter)
        return size

    @staticmethod
    def SwapTwoCards(targets: Sequence['CardFace'], by_effect: 'Effect'):
        """
        An instruction to “swap” two components means to exchange the location of those two components.
        • A swap cannot be completed if there is not a component in both locations.
            » For example, you cannot “swap a card in your hand with the top card of your deck” if you have no cards in hand.
        • Swapping a card in hand with the top card of a deck is not considered drawing that card.
        • When swapping a card in a play area with a card in an out-of-play area, if those two cards:
            » Share a title, neither card is considered to enter or leave play. Tokens, attachments, and status cards on the previously in-play card are transferred to the other card and the other card maintains the state (ready or exhausted) of the previously in-play card.
            » Do not share a title, the in-play card is considered to leave play and the out-of-play card is considered to enter play. Tokens, attachments, and status cards on the previously in-play card are not transferred to the other card and the other card enters play ready.
        """
        def do_move(face: 'CardFace', into_area: 'Deck', index: int, from_area: 'Deck'):
            if into_area.flags.is_in_play:
                face.card.MoveToArea(into_area, by_effect)
            else:
                if into_area != from_area:
                    face.card.area.Remove(face.card)
                into_area.Insert(index, face.card)

        from game.message import Message
        face0 = targets[0]
        area0 = face0.card.area
        index0 = area0.GetIndex(face0)

        face1 = targets[1]
        area1 = face1.card.area
        index1 = area1.GetIndex(face1)

        do_move(face1, area0, index0, area1)
        do_move(face0, area1, index1, area0)

        message = Message.AfterCardsSwapDeck_Text(face0, face1)
        message.Send()

    @staticmethod
    def CountTraitNum(faces: Sequence['CardFace']) -> int:
        traits: Set[str] = set()
        for face in faces:
            for trait in face.traits:
                traits.add(trait)
        return len(traits)

    @staticmethod
    def ResolveAbility(faces: Sequence['CardFace'], player: 'Player', ability_type: 'AbilityType', by_effect: 'Effect', ref_message: 'Message2|None'=None) -> int:
        ret_value = 0
        for face in faces:
            if face.ResolveAbility(player, ability_type, by_effect, ref_message):
                ret_value += 1
        return ret_value

