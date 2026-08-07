from . import *
from typing import Final

class SenderPlayer:
    
    ################################################################################
    # Player
    ################################################################################
    class PlayerOnEvent_Text(TextMessage):
        def __init__(self, player: 'Player', message: 'Message2') -> None:
            from game.message import Message
            from game.test import Test
            super().__init__(world=player.world)
            if not Test.IsInTesting() and isinstance(message, Message.WhenPlayerInTurn):
                text = TransText("\n{player} on event {message}", player=player, message=message.GetDisplayName())
                self.Present(text, "")

    class WhenPlayerSearchingDeckCards_Text(TextMessage):
        def __init__(self, player: 'Player', deck: 'Deck', size: int|None, faces: Sequence['CardFace']) -> None:
            if len(faces) > 0 and player.world.is_game_started:
                super().__init__(world=player.world)

                cards = sorted(faces)

                from collections import Counter
                instances = [x.GetDisplayName(no_hidden=True, no_object_id=True) for x in cards]
                counted_elements = Counter(instances)
                # Create a new list with the desired format
                formatted_list: List[str] = []
                for element, count in counted_elements.items():
                    if count > 1:
                        formatted_list.append(f"{element}x{count}")
                    else:
                        formatted_list.append(element)

                text = TransText("{deck} ({size}): {cards}", deck=deck, size="All" if size == None else size, cards=', '.join(formatted_list))
                self.Present(text, "", player.GetIdentity())
            pass

    class WhenPlayerResolveMulligans(TriggerPlayerMessage):
        def __init__(self, player: 'Player') -> None:
            super().__init__(player=player)
            text = TransText("{player} resolves mulligans", player=player)
            self.Present(text, "")

    class WhenPlayerWouldDrawCard(TriggerPlayerMessage, TriggerFaceMessage, CanBeInstead):
        def __init__(self, player: 'Player', face: 'CardFace', by_effect: 'Effect') -> None:
            self.by_effect: Final = by_effect
            super().__init__(player=player, trigger=face)

    # TODO: Pre message
    class AfterPlayerDrewCards(TriggerPlayerMessage):
        def __init__(self, player: 'Player', face_deck: Dict['CardFace', 'Deck'], by_effect: 'Effect') -> None:
            from game.message import Message
            self.size = len(face_deck)
            self.drew_faces = [x for x in face_deck.keys()]
            super().__init__(player=player)
            if self.size > 0:
                message = Message.AfterCardsMoved(face_deck, by_draw_up=True)
                message.Send()
                text = TransText("{player} draws {size} card(s) ({effect})", player=player, size=self.size, effect=by_effect.this)
                self.Present(text, "draw", by_effect.this, *face_deck.keys())

    class AfterPlayerGainCards_Text(TextMessage):
        def __init__(self, player: 'Player', faces: List['CardFace'], by_effect: 'Effect') -> None:
            super().__init__(world=player.world)
            text = TransText("{player} gains {faces}", player=player, faces=faces)
            self.Present(text, "draw", by_effect.this, *faces)

    class GameAreaAddPlayer(TriggerPlayerMessage):
        def __init__(self, player: 'Player') -> None:
            super().__init__(player=player)
            text = TransText("{player} enters game area", player=player)
            self.Present(text, "activate", player.GetIdentity())

    class GameAreaAddCard(TriggerFaceMessage):
        def __init__(self, face: 'CardFace') -> None:
            # from game.message import OnEvent
            # text = TransText("{face} enters game area", face=face)
            # self.Present(text, "activate", player.GetIdentity())
            super().__init__(trigger=face)
            # self.TriggerOnEvent(OnEvent.GameArea)

    class WhenPlayerSelectHero(TriggerPlayerMessage, TriggerFaceMessage):
        def __init__(self, player: 'Player', unit: 'Identity') -> None:
            super().__init__(player=player, trigger=unit)

    class AfterPlayerSelectHeroEnd(TriggerPlayerMessage, TriggerFaceMessage):
        def __init__(self, player: 'Player', unit: 'Identity') -> None:
            super().__init__(player=player, trigger=unit)
            text = TransText("{player} selected {unit}", player=player, unit=unit)
            self.Present(text, "")

    class WhenPlayerWouldPlayCard(TriggerPlayerMessage, TriggerFaceMessage, CanBeInstead):
        def __init__(self, player: 'Player', play_effect: 'Effect', resource: 'Resources', from_area: 'Deck', is_like_in_hand: bool) -> None:
            from game.card.face.attribute.has_cost import HasCost
            self.play_face: Final = play_effect.this.CastTo(HasCost)
            self.play_effect: Final = play_effect
            self.paid_resources: Final = resource
            self.be_cancel = False
            self.from_area: Final = from_area
            self.is_like_from_hand: Final = is_like_in_hand
            super().__init__(player=player, trigger=player.GetIdentity())
            text = TransText("{player} would play {this} with ({resource})", this=play_effect.this, player=player, resource=resource)
            self.Present(text, "", play_effect.this)
            self.AddRelatedFace(player, play_effect)

        def CancelEffects(self, by_effect: 'Effect', *, discard_it: bool=False) -> bool:
            from game.operate.faces import Faces
            self.Present_Activate(None, by_effect)
            self.be_cancel = True

            if discard_it:
                Faces.DiscardAll([self.play_face], by_effect)
                # text = TransText("This effect was canceled by {by_effect}", by_effect=by_effect.this)
            return True

    class WhenPlayerPlayCard(TriggerPlayerMessage, TriggerFaceMessage, CanGainValueMessage, HasEndEventMessage):
        def __init__(self, player: 'Player', message: 'Message.WhenPlayerWouldPlayCard') -> None:
            from game.message import Message
            self.played_face: Final = message.play_face
            self.played_effect: Final = message.play_effect
            self.paid_resources: Final = message.paid_resources
            self.from_area: Final = message.from_area
            self.paid_res_effects: Final = self.played_effect.context.paid_this_res_effects
            self.is_like_from_hand: Final = message.is_like_from_hand
            # self.be_cancel = False
            # text = TransText("{player} played {this} with {resource}", this=self.card_face, player=player, resource=self.paid_resources)
            # self.Present(text, "", self.play_effect.this)
            super().__init__(player=player, trigger=player.GetIdentity(), end_event=Message.AfterPlayerPlayedCard)

        def IncreaseDamage(self, num: int, by_effect: 'Effect'):
            from game.ability.ability import AbilityType
            from game.ability.factory import AbilityFactory

            self.played_face.effect.RegisterTemp(
                AbilityFactory.WhenUnitWouldAttack(
                    AbilityType.Temp0,
                    None,
                    lambda effect, message:
                        message.DealAdditionalDamage(+2, effect),
                    by_effect=self.played_effect,
                ),
                unregister_after_exec=False,
                until_resolve_effect=self.played_effect
            )

        def IncreaseThreatRemove(self, num: int, by_effect: 'Effect'):
            from game.ability.ability import AbilityType
            from game.ability.factory import AbilityFactory

            self.played_face.effect.RegisterTemp(
                AbilityFactory.WhenUnitWouldThwart(
                    AbilityType.Temp0,
                    who_make_thwart=None,
                    operation=lambda effect, message:
                        message.RemoveAdditionalThreat(+2, effect),
                    conditions=[
                        lambda effect, message:
                            self.played_effect == message.by_effect
                    ]
                ),
                unregister_after_exec=False,
                until_resolve_effect=self.played_effect
            )

    class AfterPlayerPlayedCard(TriggerPlayerMessage, HasPreEventMessage):
        def __init__(self, player: 'Player', face: 'ClassCard', effect: 'Effect', play_message: 'Message.WhenPlayerPlayCard', message: 'Message2', from_area: 'Deck') -> None:
            self.played_face: Final = face
            self.from_area: Final = from_area
            self.on_message: Final = message
            self.play_effect: Final = effect
            self.paid_resources: Final = play_message.paid_resources
            self.paid_res_effects: Final = play_message.paid_res_effects
            self.is_like_from_hand: Final = from_area.flags.is_in_hand or play_message.is_like_from_hand
            # Render.Print(f'{player} played {face}')
            super().__init__(player=player, pre_message=play_message)
            self.AddRelatedFace(player, face, effect)

        @property
        def play_message(self) -> 'Message.WhenPlayerPlayCard':
            return self.pre_message

    class CheckEffectCondition(CheckIfMessage):
        def __init__(self, check_effect: 'Effect', by_unit: 'CardFace') -> None:
            self.effect_valid = True
            self.check_effect: Final = check_effect
            super().__init__(by_unit)
            self.AddRelatedFace(check_effect, by_unit)

        def SetEffectInvalid(self, by_effect: 'Effect'):
            self.effect_valid = False
            self.SetCauseBy(by_effect)

        def GetAttacker(self):
            bind_message = self.check_effect.bind_message
            if isinstance(bind_message, AttackerNoneMessage):
                return bind_message.attacker
            return None

    # If you want to modify the coat of a card
    # e.g. "03001b" "33019"
    class WhenCalculateEffectCost(CalculateMessage, TriggerPlayerMessage):
        def __init__(self, player: 'Player', check_effect: 'Effect', for_targets: List['CardFace']) -> None:
            self.check_effect: Final = check_effect
            self.for_targets: Final = for_targets
            self.is_play: Final = check_effect.ability.is_play
            self.cost = check_effect.ability.GetCost(check_effect, for_targets)
            super().__init__(player=player)
            self.AddRelatedFace(player, check_effect, *for_targets)

        def UpdateToCost(self, value: Literal["0"], by_effect: 'Effect'):
            self.cost = Cost(value)

        def UpdateCost(self, value: int, by_effect: 'Effect'):
            self.cost += value

        def IgnoreResourceCost(self, by_effect: 'Effect'):
            self.check_effect.context.ignore_resource_cost = True

    class CheckEffectGeneratedResources(CheckIfMessage, TriggerNonePlayerMessage):
        def __init__(self, from_effect: 'Effect', check_message: 'Message.CheckPlayerCanPayCost', res: 'Resources') -> None:
            self.for_effect: Final = check_message.paying_for_effect
            self.for_targets: Final = check_message.paying_for_targets
            self.for_target: Final = check_message.paying_for_target
            self.from_effect: Final = from_effect
            self.res = res.Copy()
            super().__init__(player=check_message.to_player, by_face=from_effect.this)
            self.AddRelatedFace(from_effect, self.for_effect)

        def SetResources(self, res: 'Resources', by_effect: 'Effect'):
            self.res = res
            self.SetCauseBy(by_effect)

    # If you want to change the resource that this card can offer
    # e.g. The power of Aspect "01079" "01072" "01062" "01055",
    class CheckPlayerCanPayCost(TriggerPlayerMessage, CheckNoneMessage):
        def __init__(self, player: 'Player', for_effect: 'Effect', cost: 'Cost', for_targets: List['CardFace']) -> None:
            from game.card.face.base import ClassCard
            self.paying_for_effect: Final = for_effect
            self.paying_for_targets: Final = for_targets
            self.paying_for_target: Final = for_targets[0] if for_targets != [] else None
            self.cost: Final = cost
            # if self.for_effect.ability.type.is_play:
            # Fix "03016" with "03018"
            if ClassCard.IsType(self.paying_for_effect.this):
                paying_for_card = self.paying_for_effect.this
            else:
                paying_for_card = None
            self.paying_for_card: Final = paying_for_card
            self.can_pay_effects: List[Tuple['Effect', 'Resources', 'Effect']] = []
            super().__init__(player=player, by_face=self.paying_for_target)

        def AddPayment(self, cost_effect: 'Effect', res: 'Resources', check_effect: 'Effect'):
            self.can_pay_effects.append((cost_effect, res, check_effect))

    # Generate resource like "03010" and discard gain resource both use this message
    # It will have bugs if a card have 2 method to generate resources
    class WhenPlayerPayingResources(TriggerPlayerMessage):
        def __init__(self, player: 'Player', by_effect: 'Effect', for_effect: 'Effect', for_targets: List['CardFace'], expected_res: 'Resources', cost_check_effect: 'Effect') -> None:
            from game.card.face.base import ClassCard
            self.by_effect: Final = by_effect
            self.for_effect: Final = for_effect
            self.for_targets: Final = for_targets
            self.for_target: Final = for_targets[0] if for_targets != [] else None
            self.expected_res: Final = expected_res # See `CheckThisCanPayCost`
            self.generated_res: 'Resources'
            self.cost_check_effect: Final = cost_check_effect
            # if self.for_effect.ability.type.is_play:
            # Fix "03016" with "03018"
            if ClassCard.IsType(self.for_effect.this):
                paying_card = self.for_effect.this
            else:
                paying_card = None
            self.paying_card: Final = paying_card
            super().__init__(player=player)
            # self.AddRelatedFace(player, by_effect, for_effect, *for_targets)

        def SetGenerateRes(self, res: 'Resources'):
            self.generated_res = res

        # def UpdateCost(self, diff: int, by_effect: 'Effect'):
        #     self.for_effect.cost_for_different_target.UpdateCost(self.for_target, diff)

    # class WhenPlayerLikePayingResources(WhenPlayerPayingResources):
    #     def __init__(self, player: 'Player', for_effect: 'Effect') -> None:
    #         super().__init__(player, for_effect)

    # class AfterPlayerPaidResources(Message2):
    #     def __init__(self, player: 'Player', for_effect: 'Effect', res: 'Resources') -> None:
    #         self.player = player
    #         self.for_effect = for_effect
    #         self.res = res

    # class WhenPlayerPhaseEnd(TriggerPlayerMessage):
    #     def __init__(self, player: 'Player') -> None:
    #         super().__init__(player=player)

    class WhenPlayerTurnBegin(TriggerPlayerMessage, HasEndEventMessage):
        def __init__(self, player: 'Player') -> None:
            from game.message import Message
            super().__init__(player=player, end_event=Message.AfterPlayerTurnBegin)

    class AfterPlayerTurnBegin(TriggerPlayerMessage, HasPreEventMessage):
        def __init__(self, player: 'Player', message: 'Message.WhenPlayerTurnBegin') -> None:
            super().__init__(player=player, pre_message=message)

    class WhenPlayerTurnEnd(TriggerPlayerMessage, HasEndEventMessage):
        def __init__(self, player: 'Player', round_id: int) -> None:
            from game.message import Message
            self.round_id = round_id # [1...]
            super().__init__(player=player, end_event=Message.AfterPlayerTurnEnd)
            text = TransText("\n--- {player} Turn End ---", player=player)
            self.Present(text, "phase")

    class AfterPlayerTurnEnd(TriggerPlayerMessage, HasPreEventMessage):
        def __init__(self, player: 'Player', message: 'Message.WhenPlayerTurnEnd') -> None:
            super().__init__(player=player, pre_message=message)

    class WhenPlayerInTurn(TriggerPlayerMessage):
        def __init__(self, player: 'Player', turn_cnt: int, **kwargs: Any) -> None:
            self.turn_cnt: Final = turn_cnt
            super().__init__(player=player, **kwargs)
            if turn_cnt != -1:
                text = TransText("\n--- {player}'s Turn ({turn_cnt}) ---", player=player, turn_cnt=turn_cnt)
                self.Present(text, "phase")

    class WhenPlayerLikeInTurn(WhenPlayerInTurn, LikeFakeMessage, NoSendMessage):
        def __init__(self, player: 'Player', by_effect: 'Effect') -> None:
            self.by_effect: Final = by_effect
            super().__init__(player=player, turn_cnt=-1)

    class WhenPlayerEndPhase(TriggerPlayerMessage):
        def __init__(self, player: 'Player', **kwargs: Any) -> None:
            super().__init__(player=player, **kwargs)
            text = TransText("{player} End Phase", player=player)
            self.Present(text, "")

    class WhenPlayerEliminated(Message2):
        def __init__(self, player: 'Player') -> None:
            super().__init__(world=player.world)
            text = TransText("{player} eliminated", player=player)
            self.Present(text, "")

    # Only for `ForChoiceAbility`
    class WhenPlayerChooseAbility(TriggerPlayerMessage, NoSendResolve):
        def __init__(self, player: 'Player', by_effect: 'Effect', step: Tuple[int, int], for_second_target: bool) -> None:
            self.by_effect: Final = by_effect
            self.step: Final = step
            self.for_second_target: Final = for_second_target
            # self.Present(None, "", by_effect.this)
            super().__init__(player=player)

    class WhenPlayerWouldBeDealtEncounterCard(TriggerPlayerMessage, HasEndEventMessage, CanBeInstead):
        def __init__(self, player: 'Player', by_effect: 'Effect', face: 'CardFace') -> None:
            from game.message import Message
            self.by_effect: Final = by_effect
            self.face: Final = face
            super().__init__(player=player, end_event=Message.AfterPlayerDealEncounterCard)

    class AfterPlayerDealEncounterCard(TriggerPlayerMessage, HasPreEventMessage):
        def __init__(self, player: 'Player', message: 'Message.WhenPlayerWouldBeDealtEncounterCard') -> None:
            self.by_effect: Final = message.by_effect
            super().__init__(player=player, pre_message=message)
            # text = TransText("{player} deals {size} encounter card(s) ({by_effect})", player=player, size=1, by_effect=self.by_effect.this, pre_message=message)
            # self.Present(text, "", player.GetIdentity(), message.face)

        @property
        def would_message(self) -> 'Message.WhenPlayerWouldBeDealtEncounterCard':
            return self.pre_message

    # For `AbilityType.Special`
    # e.g. "01046" "01047" "01048" "01049"
    class WhenResolveSpecialAbility(TriggerFaceMessage, TriggerNonePlayerMessage):
        def __init__(self, face: 'CardFace', name: str, sequence: Sequence['CardFace'], to_player: 'Player|None', by_effect: 'Effect') -> None:
            self.face: Final = face
            self.sequence: Final = sequence
            self.by_effect: Final = by_effect
            self.ability_name: Final = name
            super().__init__(trigger=face, player=to_player, world=face.card.world)
            text = TransText("{player} resolves {face}'s {ability_name} ({by_effect})", player=to_player, face=face, ability_name=name, by_effect=by_effect.this)
            self.Present(text, "", face, by_effect.this)

        def GetToPlayer(self) -> 'Player|None': # type: ignore
            return self.to_player

    class WhenResolvePreparationAbility(TriggerFaceMessage, AttackerNoneMessage, TriggerNonePlayerMessage):
        def __init__(self, face: 'CardFace', to_player: 'Player|None', by_effect: 'Effect') -> None:
            from game.message import Message
            self.face: Final = face
            self.by_effect: Final = by_effect
            self.would_atk_message_unit: Final = by_effect.bind_message.CastTo(Message.WhenUnitWouldAttackUnit) if by_effect.bind_message else None
            self.would_atk_message: Final = self.would_atk_message_unit.would_atk_message if self.would_atk_message_unit else None
            being_atk_message: Final = self.would_atk_message_unit.being_atk_message if self.would_atk_message_unit else None
            self.damage: Final = self.would_atk_message_unit.property.GetDamage(face) if self.would_atk_message_unit else 0
            super().__init__(trigger=face, player=to_player, being_atk_message=being_atk_message, world=face.card.world)
            text = TransText("{player} resolves {face}'s Preparation ({by_effect})", player=to_player, face=face, by_effect=by_effect.this)
            self.Present(text, "", face, by_effect.this)

        def GetToPlayer(self) -> 'Player|None': # type: ignore
            return self.to_player

        def PreventAllDamage(self, by_effect: 'Effect'):
            if self.would_atk_message:
                self.would_atk_message.PreventDamage("All", by_effect)
            # self.Present_Activate(None, by_effect)

        def AfterThisAttack(self, action: Callable[[], None]):
            from cards.pack import RunAt
            if self.would_atk_message:
                RunAt.AfterEventEnd(self.by_effect, self.would_atk_message, action)

        def ResolveThisAttackToInstead(self, unit: 'Unit2', by_effect: 'Effect'):
            if self.would_atk_message_unit:
                self.would_atk_message_unit.ChangeTarget(unit, by_effect)

    class CheckIfAllyCountLimit(CheckIfMessage):
        def __init__(self, face: 'CardFace') -> None:
            self.not_count = False
            self.check_ally: Final = face
            super().__init__(by_face=face)

        def SetNotCountAlly(self, by_effect: 'Effect'):
            self.not_count = True
            self.SetCauseBy(by_effect)

    ################################################################################
    #
    # No pre message
    class AfterPlayersOrderChange(Message2):
        def __init__(self, world: 'World', by_effect: 'Effect') -> None:
            super().__init__(world=world)

    class CheckIfFaceCountHandSize(TriggerPlayerMessage, TriggerFaceMessage, CheckIfMessage):
        def __init__(self, player: 'Player', face: 'CardFace') -> None:
            self.count_hand_size = True
            super().__init__(player=player, trigger=face, by_face=face)

        def SetNotCountHandSize(self, by_effect: 'Effect'):
            self.count_hand_size = False
            self.SetCauseBy(by_effect)

    class WhenCountingResourcesOnCards(CalculateMessage):
        def __init__(self, face: 'ClassCard', from_deck: 'Deck|None') -> None:
            self.face = face
            self.from_deck = from_deck
            self.return_res: Resources = face.printed_resource_internal
            super().__init__(world=face.card.world)

        def SetReturnValue(self, res: Resources, effect: 'Effect'):
            self.return_res = res
            # self.Present_Activate(None, effect)

