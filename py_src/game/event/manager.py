from typing import Final, TypeAlias
from core import *
from game.ability import *
from game.message import *
from game.player import *
from game.card.face import *
from engine.job import JobManager
from engine.log import DebugLog, Log
from game.world import *
from game.event.counter import EventCounter
from engine.config import ConfigVariables
from game.message.message_type import CardStateUpdatedMessage
from engine.controller.module.undo import UndoModule

FastUndoHandle = UndoModule.UndoHandle|None

FILE_BASED_FAST_UNDO    = ConfigVariables.Bool('file_based_fast_undo', False)
CACHE_BASED_FAST_UNDO   = ConfigVariables.Bool('cache_based_fast_undo', False)

class EventManager:

    CATEGORY: TypeAlias = Literal["Statistics", "Rule", "Paying", "Forced", "Optional"]
    CATEGORY_LIST: List['EventManager.CATEGORY'] = ["Statistics", "Rule", "Paying", "Forced", "Optional"]

    def __init__(self, world: 'World') -> None:
        self.effects: Dict[EventManager.CATEGORY, Dict[Type['Message2'], Dict['TimingPriority', List['Effect']]]] = {
            "Statistics": {},
            "Rule": {},
            "Paying": {},
            "Forced": {},
            "Optional": {},
        }

        self.registered_message_type: Dict[Type['Message2'], int] = {}

        self.event_size = EventCounter()
        # self.last_message: Message2|None = None
        # self.process_message: Message2|None = None
        self.world: Final = world
        self.new_effect_created: bool = False

        self.debug_log = DebugLog("NoSendResolve")
        self.debug_log_both = DebugLog("Both")
        self.debug_log_only_local = DebugLog("Only Local")
        self.debug_log_only_global = DebugLog("Only Global")

        self.stack_message: List['CardStateUpdatedMessage'] = []

    def GetEffectCategory(self, effect: 'Effect', event: Type['Message2']) ->  Literal["Forced", "Rule", "Paying", "Optional", "Statistics"]:
        from game.message import Message
        if effect.ability.flags.is_statistics:
            return "Statistics"
        if effect.is_nonkeyword:
            return "Rule"
        if event is Message.WhenPlayerPayingResources:
            return "Paying"
        if effect.is_rule:
            return "Rule"
        if effect.ability.flags.is_temp:
            return "Rule"
        if effect.is_forced:
            return "Forced"
        return "Optional"

    def AddEffectsList(self, category: 'EventManager.CATEGORY', event: Type['Message2'], priority: 'TimingPriority', add_effect: 'Effect') -> None:
        found_dict = self.effects[category]
        if event not in found_dict:
            found_dict[event] = {}
        if priority not in found_dict[event]:
            found_dict[event][priority] = []
        found_dict[event][priority].append(add_effect)

    def FindEffectsList(self, category: 'EventManager.CATEGORY', event: Type['Message2'], priority: 'TimingPriority') -> List['Effect']:
        found_dict = self.effects[category]
        if event not in found_dict:
            return []
        if priority not in found_dict[event]:
            return []
        return found_dict[event][priority]

    def HasRegisteredEvent(self, event: Type['Message2']) -> int:
        if event in self.registered_message_type:
            return self.registered_message_type[event]
        return 0

    def RegisterEffect(self, effect: 'Effect'):
        effect.is_unregister = False

        self.new_effect_created = True

        # if effect.ability.type.ability_type == AbilityType.CheckResource:
        #     pass

        if effect.is_local:
            events = Types.UnionTypeExtract(effect.ability.when)
            for event in events:
                category = self.GetEffectCategory(effect, event)
                assert category in ["Forced", "Paying", "Rule", "Optional"]
        else:
            events = Types.UnionTypeExtract(effect.ability.when)
            for event in events:
                if event in self.registered_message_type:
                    self.registered_message_type[event] += 1
                else:
                    self.registered_message_type[event] = 1

                self.event_size.Add(event)
                category = self.GetEffectCategory(effect, event)
                assert category != "Paying"
                self.AddEffectsList(category, event, effect.ability.priority, effect)

    def UnRegisterEffect(self, effect: 'Effect'):
        effect.is_unregister = True
        # TODO:
        # if effect.is_local:
        #     return
        events = Types.UnionTypeExtract(effect.ability.when)
        for event in events:
            self.registered_message_type[event] -= 1
            category = self.GetEffectCategory(effect, event)
            effects_list = self.FindEffectsList(category, event, effect.ability.priority)
            if effect.is_local:
                assert effect not in effects_list
            else:
                effects_list.remove(effect)

    ################################################################################
    #
    def RegisterPlayRule(self):
        from game.rule.gameplay import GetGamePlayRules
        from game.rule.statistics import GetStatisticsRule
        from game.rule.achievement import GetPlayAchievement
        # from game.rule.challenges import GetChallengeRules
        from game.card.factory import CardFactory
        from game.effect.rule import GameRule
        world = self.world
        insert = CardFactory.GenerateCard('rule_a,rule_b', world.area_insert, world).face.CastTo(Insert)
        world.insert = insert
        for ability in GetGamePlayRules():
            insert.effect.Registers(ability)
        from engine import Engine
        if Engine.statistics.CanRegisterAbility():
            for ability in GetStatisticsRule():
                insert.effect.Registers(ability)
            for ability in GetPlayAchievement():
                insert.effect.Registers(ability)
        # insert.PutIntoPlay(None, effect)
        effect = GameRule(insert)
        for challenge in world.scene.campaign.challenges:
            challenge = CardFactory.GenerateCard(challenge, world.area_insert, world).face
            challenge.PutIntoPlay("FirstPlayer", effect)

    ################################################################################
    #
    def ProcessEffect(self, effect: 'Effect', message: 'Message2', priority: 'TimingPriority'):
        from game.player import Player
        from game.message.sender.sender import TriggerNonePlayerMessage

        # self.process_message = message

        processed = False
        if effect.ability.selectors:
            owner = effect.this.GetOwner()
            player = None
            if isinstance(owner, Player):
                player = owner
            elif isinstance(message, TriggerNonePlayerMessage):
                # and not effect.is_forced:
                player = message.to_player
            if player:
                # if effect.is_forced:
                #     faces = player.AskChooseSelect2(
                #         effect.all_legal_targets,
                #         effect.target_range,
                #         effect,
                #         peek=False,
                #         not_move=True,
                #         not_shuffle=True,
                #     )
                #     effect.targets = faces
                # else:
                    cheat = True
                    while cheat:
                        _, cheat = player.ChoiceAndSpellEffect([effect], message, priority, forced=True)
                    processed = True
            else:
                assert effect.is_forced, f"{effect=}"
                assert effect.context.target_range[0] == effect.context.target_range[1], f"{effect.context.target_range=}"
                assert len(effect.context.all_legal_targets) == effect.context.target_range[1], f"{effect.context.all_legal_targets=}"
                effect.context.targets_internal = effect.context.all_legal_targets[:]
        if not processed:
            effect.ResolveSelf(message, effect)

    ################################################################################
    #
    def ProcessRuleEffect(self, message: 'Message2', rule_effects: List['Effect'], priority: 'TimingPriority', undo_handle: 'FastUndoHandle') -> 'GAME_OVER':
        # Rule and Temp
        for effect in rule_effects:

            if EventManager.FilterAvailableEffects(message, [effect], None, self.world, undo_handle):
                self.ProcessEffect(effect, message, priority)

            if self.world.is_game_over:
                break

        return self.world.is_game_over

    def ProcessPayingEffect(self, message: 'Message.WhenPlayerPayingResources', resources_effect: 'Effect', priority: 'TimingPriority') -> 'GAME_OVER':
        # Resources
        resources_effects = EventManager.FilterAvailableEffects(message, [resources_effect], message.to_player, self.world, None)
        assert len(resources_effects) == 1
        self.ProcessEffect(resources_effects[0], message, priority)

        return self.world.is_game_over

    def ProcessOptionalEffect(self, message: 'Message2', optional_effects: List['Effect'], local_optional_effects: List['Effect'], priority: 'TimingPriority') -> 'GAME_OVER':
        from game.message import Message
        from game.effect.effect_failure import EffectFailure
        # from game.object.object_manager import ObjectManager

        is_play_turn = type(message) == Message.WhenPlayerInTurn

        players = self.world.const_players[:]

        def process_player(player: 'Player'):
            processed_effects: List['Effect'] = []
            nonlocal optional_effects

            while True:
                if is_play_turn:
                    self.new_effect_created = False
                if self.world.is_game_over:
                    break

                # Clean all resource effect
                # ObjectManager.EmptyPayingEffect()
                # ObjectManager.ResetPayingEffect()

                def fast_undo():
                    controller_manager = self.world.controller_manager
                    if controller_manager.undo.DoNotCheckFastUndo():
                        Log.DebugSilent("MESSAGE", f"{'-'*30}")
                        Log.DebugSilent("MESSAGE", f"{len(self.world.object_manager.message_dict)=}")
                        Log.DebugSilent("MESSAGE", f"{'-'*30}")
                        return optional_effects

                    step_id = controller_manager.replay.current_step_id

                    # We only check "Optional" when not skipping
                    from game.scene.replay import CommandDescriptor
                    is_puzzle = message.world.scene.is_puzzle
                    found_effects: List['Effect'] = optional_effects[:]
                    operation, read_ok = controller_manager.replay.GetReplayOperation(is_puzzle, check_crc=False)
                    replay_id = None
                    fast_undo_result = ""
                    if read_ok:
                        if operation:
                            from colorama import Fore
                            replay_id = f"{Fore.BLUE}{operation.effect.id}{Fore.RESET}"
                            if operation.effect.id.startswith(":"):
                                fast_undo_result = f"Skip debug command {replay_id}"
                            elif operation.effect.id != '':
                                effect_ids = CommandDescriptor.FindNewEffectIdInternal(operation.effect.id, optional_effects)
                                if effect_ids:
                                    found_effects.clear()
                                    for effect_id in effect_ids:
                                        for effect in optional_effects:
                                            if effect.object_id == effect_id:
                                                found_effects.append(effect)
                                                break
                                    assert found_effects
                                    fast_undo_result = f"{replay_id}, {len(optional_effects)} -> {len(found_effects)}"
                                else:
                                    fast_undo_result = f"{replay_id}, {len(optional_effects)} ->"
                            else:
                                # In this case, we have to check all optional effect
                                # to find is user cancel this effect in this event or others
                                fast_undo_result = f"{replay_id}, {len(optional_effects)} -> Input Cancel"
                    Log.DebugSilent("FAST_UNDO", f'{step_id}, {fast_undo_result}')
                    return found_effects

                if not FILE_BASED_FAST_UNDO.value:
                    filtered_effects = EventManager.FilterAvailableEffects(message, optional_effects, player, self.world, None)
                else:
                    fast_undo_effects = fast_undo()
                    if not fast_undo_effects:
                        return
                    filtered_effects = EventManager.FilterAvailableEffects(message, fast_undo_effects, player, self.world, None)

                for effect in filtered_effects[:]:
                    if effect in processed_effects:
                        filtered_effects.remove(effect)
                        effect.failures.Set(player, EffectFailure.AlreadyProcessed)

                if filtered_effects == []:
                    break
                else:
                    from game.message import Message
                    Message.PlayerOnEvent_Text(player, message)

                    filtered_effects = sorted(filtered_effects, key=lambda e: e.object_id)

                    forced = "Forced_Action" if any(effect.ability.flags.is_forced_action for effect in filtered_effects) else False
                    effect, is_cheating = player.ChoiceAndSpellEffect(filtered_effects, message, priority, forced)

                    if not is_cheating and not effect:
                        break
                    if is_cheating or self.new_effect_created:
                        if is_play_turn:
                            self.new_effect_created = False

                        if type(message) in self.effects["Optional"] and \
                            priority in self.effects["Optional"][type(message)]:

                            optional_effects = [x for x in self.effects["Optional"][type(message)][priority] if x.ability.priority == priority] + local_optional_effects
                            optional_effects = Types.RemoveDuplicates(optional_effects)

                        if is_cheating:
                            assert effect == None, f"{effect=}"

                    if not is_play_turn and effect:
                        # Fix for "05005" and "05001a"
                        if not Event.IsType(effect.this):
                            processed_effects.append(effect)
                        pass

        JobManager.Simultaneous(process_player, players)

        return self.world.is_game_over

    @staticmethod
    def SelectForcedEffect(forced_effects: List['Effect'], ask_first_player: 'Callable[[List[Effect]], Effect|None]') -> 'Effect':
        """Which of several simultaneous forced abilities initiates next.

        The Rules Reference: *"If two or more forced abilities would initiate at
        the same moment, the first player determines the order in which the
        abilities initiate, **regardless of who controls the cards bearing those
        abilities**."*

        A seam rather than an inline block because the caller needs a live world
        and a real player to reach it, and the rule above is worth testing on its
        own. `ask_first_player` is the prompt; it returns `None` when the player
        declines, and the fallback is list order -- which is `Effect.object_id`
        order after MARVEL-31, so creation order rather than the allocator's.

        **The choice is over effects, never over faces.** Two earlier defects
        both came from selecting over faces and re-deriving the effect:

        - Delay abilities are excluded from the choice, so an index into the
          faces was an index into the *filtered* list. Reading the unfiltered
          `forced_effects` with it resolved a different ability than the one
          chosen -- for `[normal, delay, normal]`, picking the second normal
          ability resolved the excluded delay ability. That was MARVEL-39.
        - A face cannot name *which* of a card's abilities was picked, so a
          batch that was all on one card was never put to the first player at
          all; the engine took the first entry. The rule above draws no such
          distinction. That was MARVEL-40, and it is why there is no longer an
          `is_on_the_same_card` branch here.

        Only the count decides whether to ask now. One candidate is not a tie.
        """
        candidates = [x for x in forced_effects if not x.ability.flags.is_delay_ability]
        # The caller only enters this path when `forced_effects[0]` is not a
        # delay ability, so there is always at least one candidate. Assert it
        # rather than inherit it: this method is reachable on its own.
        assert candidates, f"{forced_effects=}"

        if len(candidates) == 1:
            return candidates[0]

        chosen = ask_first_player(candidates)
        if chosen == None:
            return candidates[0]
        assert chosen in candidates, f"{chosen=} {candidates=}"
        return chosen

    @staticmethod
    def ForcedOrderLabels(candidates: List['Effect']) -> List[str]:
        """One prompt label per candidate, all distinct. **Replay-visible.**

        The label is what the recorded command carries, and replay re-resolves a
        recorded effect through `CommandDescriptor.FindNewEffectIdInternal`,
        which narrows by the *card* the effect sits on and then, only if that
        left more than one, by display name. Every option here is built on the
        first player's identity, so they all share one card and the display name
        is the only thing separating them.

        That is fine until two forced abilities on one card share a display name
        -- exactly the case MARVEL-40 makes selectable. Both options would then
        be indistinguishable in the recording and replay would take the first,
        silently resolving the other ability.

        So a duplicated name gets a 1-based ordinal in candidate order, which is
        `Effect.object_id` order. A name that is already unique is left alone, so
        prompts and recordings do not churn where there was never an ambiguity.
        The rule is a function of the batch, not of global allocation, so a port
        reproduces it without reproducing an id counter.
        """
        names = [effect.GetDisplayName() for effect in candidates]
        labels: List[str] = []
        seen: Dict[str, int] = {}
        for name in names:
            if names.count(name) == 1:
                labels.append(name)
            else:
                seen[name] = seen.get(name, 0) + 1
                labels.append(f"{name} #{seen[name]}")
        return labels

    def AskForcedOrder(self, first_player: 'Player', candidates: List['Effect']) -> 'Effect|None':
        """Ask the first player which forced ability initiates next.

        Built on `ChooseAbilities`, which is the same machinery `AskChooseFace`
        reaches through `AskChooseSelect` -- one `ForChoiceAbility` per option
        rather than one selector over faces. That is the whole reason this can
        express a choice between two abilities on a single card, which a face
        selector cannot.

        `Ties` is carried as the `by_effect` so the prompt keeps naming the rule
        it is applying, which `AskChooseOneText` would have dropped in favour of
        a bare `GameRule`.
        """
        from game.ability.factory import AbilityFactory
        from game.effect.rule import Ties

        labels = EventManager.ForcedOrderLabels(candidates)
        abilities = [AbilityFactory.ForChoiceAbility(label) for label in labels]

        effects = first_player.ChooseAbilities(
            Ties("Forced abilities would initiate at the same moment", world=self.world),
            *abilities,
        )
        if not effects:
            return None

        # `ChooseAbilitiesHelper` wraps each ability we passed in an `Effect`, so
        # the chosen effect's `ability` *is* one of `abilities` -- mapping back is
        # a position lookup rather than a callback. Compared by identity because
        # `Ability` does not define `__eq__` and two options may carry equal
        # content.
        chosen_ability = effects[0].ability
        for index, ability in enumerate(abilities):
            if ability is chosen_ability:
                return candidates[index]
        return None

    def ProcessForcedEffect(self, message: 'Message2', forced_effects: List['Effect'], priority: 'TimingPriority', undo_handle: 'FastUndoHandle') -> 'GAME_OVER':
        from game.message.sender.sender import CanBeInstead
        from game.ability.ability import TimingPriority
        from game.message import Message

        # The order of `forced_effects` is by the id, which means when this card be created
        # And we didn't make them same to the hand card order
        #
        # That invariant is now actually held: the local effects arrive sorted
        # by `object_id` from `FindLocalEffects`, and the globally registered
        # ones that may be appended after them are in registration order.
        # Before MARVEL-31 the local part was in memory-address order.

        # if isinstance(message, Send.CheckPlayerCanPayCost):
        #     pass

        # Forced
        while True:
            forced_effects = EventManager.FilterAvailableEffects(message, forced_effects, None, self.world, undo_handle)

            if forced_effects == []:
                break
            if self.world.is_game_over:
                break
            if isinstance(message, CanBeInstead) and message.is_be_instead:
                break

            def check_is_resources(forced_effect: 'Effect'):
                assert not forced_effect.ability.flags.is_resource
                assert not forced_effect.ability.flags.is_discard_pay
                return forced_effect.ability.flags.is_check_pay

            first_effect = forced_effects[0]

            if first_effect.ability.priority != TimingPriority.Status and \
                not isinstance(message, Message.WhenGameBeginSetup) and \
                not check_is_resources(first_effect) and \
                not first_effect.ability.flags.is_delay_ability:
                first_player = self.world.GetFirstPlayer()
                effect = EventManager.SelectForcedEffect(
                    forced_effects,
                    lambda candidates: self.AskForcedOrder(first_player, candidates),
                )
            else:
                effect = first_effect

            forced_effects.remove(effect)

            # Clean
            effect.context.targets_internal = []

            if effect.IsPlayerInitiator() and len(effect.context.all_legal_targets) > 0:
                initiator = effect.GetInitiator()
                _, is_cheating = initiator.ChoiceAndSpellEffect([effect], message, priority, True)
                if is_cheating:
                    forced_effects.append(effect)
            else:
                self.ProcessEffect(effect, message, priority)


        if self.world.is_game_over:
            return True
        if isinstance(message, CanBeInstead) and message.is_be_instead:
            return True
        return False

    ################################################################################
    #
    @staticmethod
    def SimpleCheckEffects(message: 'Message2', effects: Sequence['Effect'], asked_player: 'Player|None', world: 'World', undo_handle: 'FastUndoHandle') -> List['Effect']:
        from game.card.face.base import Unit2
        from game.card.face.base import Scheme2
        from game.card.face.card_type import Upgrade
        from game.card.face.card_type import Minion
        from game.card.face.card_type import Support
        from game.card.face.card_type import Attachment
        from game.card.face.card_type import Environment
        from game.card.face.card_type import Resource
        from game.card.face.card_type import Event
        from game.card.face.card_type import Obligation
        from game.effect.effect import EffectFailure
        from game.message import Message
        from game.message import TriggerPlayerMessage

        if CACHE_BASED_FAST_UNDO.value and undo_handle:
            check_effects_list = undo_handle.GetAvailableEffects(effects)
            step_id = undo_handle.step_id
            if check_effects_list != None:
                if check_effects_list and \
                    len(effects) != len(check_effects_list):
                    Log.DebugSilent("UNDO_HANDLE", f"{step_id} {message}: {len(effects)} -> {len(check_effects_list)}")
                effects = check_effects_list

        effects_list: List['Effect'] = []
        for effect in effects:
            if effect.is_unregister:
                continue

            assert isinstance(message, effect.ability.when), f"{message=} {effect.ability.when=}"

            if effect.this.is_treat_as_if_blank and \
                not effect.ability.ignore.treat_as_if_blank and \
                not isinstance(message, Message.WhenCardTreatAsIfBlank):
                effect.failures.Set(asked_player, EffectFailure.TreatAsBlank)
                continue
            if effect.this.card.area.flags.is_removed and \
                not effect.ability.ignore.be_removed and \
                type(message) is not Message.WhenPlayerLikeInTurn:
                # not effect.this.IsName("rule") and \
                effect.failures.Set(asked_player, EffectFailure.IsRemoved)
                continue
            # if effect.this.card.IsAsOtherCard():
            #     effect.AddFailureReason(asked_player, 'as other card')
            #     continue

            def check_initiator() -> Tuple[bool, str]:
                this = effect.this
                current_player = world.GetCurrentPlayer()
                check_player = None

                if effect.ability.flags.is_resource or effect.ability.flags.is_discard_pay:
                    check_player = message.CastTo(Message.WhenPlayerPayingResources).GetToPlayer()
                elif effect.ability.any_player_can_trigger_this_when:
                    if isinstance(message, TriggerPlayerMessage):
                        check_player = message.to_player
                    else:
                        check_player = current_player
                elif isinstance(message, Message.WhenPlayerPayingResources):
                    if Event.IsType(message.for_effect.this) and message.for_effect.this.alliance:
                        check_player = message.to_player
                    else:
                        # Fix "50014"
                        check_player = message.to_player # this.GetControlByOrOwner()
                elif isinstance(message, Message.WhenCardBeSpendAsResource):
                    # Fix "28019"
                    check_player = message.to_player
                elif effect.ability.is_choose:
                    return True, ""
                elif Obligation.IsType(this):
                    if this.card.area.flags.is_obligations_area:
                        check_player = this.GetGaveToPlayer()
                    else:
                        check_player = current_player
                elif Attachment.IsType(this) and \
                    not Identity.IsType(this.bind_face):
                    # Fix "21179"
                    check_player = current_player
                elif Minion.IsType(this):
                    # Fix "27131"
                    check_player = current_player
                elif Scheme2.IsType(this):
                    # Fix "27081"
                    check_player = current_player
                elif Environment.IsType(this):
                    # Fix "27077a"
                    check_player = current_player
                elif effect.ability.flags.is_play_turn_option or \
                    effect.ability.flags.is_basic_power or \
                    effect.ability.flags.is_interrupt or \
                    effect.ability.flags.is_response or \
                    effect.ability.flags.is_resource or \
                    effect.ability.flags.is_action or \
                    effect.ability.flags.is_ask or \
                    effect.ability.is_play or \
                    effect.ability.flags.is_discard_pay:
                    if this.IsInPlay():
                        check_player = this.GetControlByOrOwner()
                    else:
                        check_player = this.card.area.GetOwner()
                elif Event.IsType(this):
                    assert False, f"{effect}"
                elif Resource.IsType(this):
                    assert False, f"{effect}"
                elif Support.IsType(this):
                    assert False, f"{effect}"
                elif Upgrade.IsType(this):
                    assert False, f"{effect}"
                    # return this.GetController() == asked_player
                    # Fix Spider-Tracer "01007"
                    # if isinstance(this.GetController(), Scenario):
                    #     # TODO: bug, if a card become an "Ultron Facedown DRONE",
                    #     # it's "this.card.area.owner" will not update
                    #     effect.initiator = this.GetOwner()
                    # else:
                    #     effect.initiator = this.GetController()
                elif Unit2.IsType(this):
                    assert False, f"{effect}"
                    # Fix "20016"
                    # return current_player == asked_player


                elif this.card.area.flags.is_processing:
                    assert False, f"{effect}"
                    # return True
                    # effect.initiator = this.GetOwner()
                else:
                    if asked_player == None:
                        return True, ""
                    else:
                        assert False, f"{this=}"
                    # return this.GetController() == asked_player
                    # this_owner = this.GetOwner()
                    # if isinstance(this_owner, Scenario):
                    #     effect.initiator = this_owner
                    # else:
                    #     effect.initiator = this.GetController()

                return check_player == asked_player, f"{check_player}"

            # Need initiator
            # if not effect.HasCostTargets():
            #     effect.failure_reason = "no cost target (simple check)"
            #     continue

            if not effect.is_forced:
                ok, error = check_initiator()
                if not ok:
                    # if effect.this.card.object_id == 128:
                    #     check_initiator()
                    #     pass
                    effect.failures.SetText(asked_player, f"initiator error, check: {error}, ask: {asked_player}")
                    continue

            effects_list.append(effect)

        return effects_list

    @staticmethod
    # Will also setup `all_legal_targets`
    def FilterAvailableEffects(message: 'Message2', effects: Sequence['Effect'], asked_player: 'Player|None', world: 'World', undo_handle: 'FastUndoHandle') -> List['Effect']:
        from game.message.sender.sender import CanBeInstead
        from game.message import Message
        from game.player import Player

        if isinstance(message, CanBeInstead) and message.is_be_instead:
            return []

        checked_effects = EventManager.SimpleCheckEffects(message, effects, asked_player, world, undo_handle)

        for effect in checked_effects:
            effect.this.card.ui.unavailable.Reset()

        def check():

            effects_list: List['Effect'] = []
            for effect in checked_effects:
                # init initiator
                effect.context.bind_message = message
                # if effect.ability.any_player_can_do_this and asked_player:
                #     effect.initiator = asked_player
                if asked_player != None:
                    effect.context.initiator = asked_player
                else:
                    effect.context.initiator = effect.this.GetControlByOrOwner()

                effect.context.ask_player = asked_player
                effect.context.ResetBeforeCondition()
                if asked_player == None and isinstance(effect.initiator, Player):
                    effect.context.ask_player = effect.initiator
                else:
                    effect.context.ask_player = asked_player
                if not effect.checker.CheckCondition(message, effect.context.ask_player):
                    if effect.ability.when is Message.WhenPlayerChooseAbility:
                        if effect.ability.selectors and \
                            effect.ability.selectors[0]:
                            effect.ability.selectors[0].IfSelectTargetFailure(effect)
                    continue

                effects_list.append(effect)
            return effects_list

        effects_list = check()

        if CACHE_BASED_FAST_UNDO.value and undo_handle:
            controller_manager = message.world.controller_manager
            controller_manager.undo.PushFastUndo(message, effects_list)
        return effects_list

    ################################################################################
    #
    @staticmethod
    def GatherLocalEffects(message: 'Message2') -> List['Effect']:
        """The on-card effects a message triggers, in the order they are found.

        That order comes from `Message2.related_faces`, a `Set[CardFace]`, and
        `CardFace` defines no `__hash__` -- so it is memory-address order and
        nothing may depend on it. Callers want `FindLocalEffects`. This is
        separate only so `tools/determinism/probe_local_effect_order.py` can
        measure what the sort changes without copying this loop.
        """
        from game.message import Message

        if isinstance(message, Message.WhenPlayerPayingResources):
            return [message.by_effect]

        local_effects: List['Effect'] = []
        for face in message.related_faces:
            for check_effect in face.effect.local_effects:
                if isinstance(message, check_effect.ability.when):
                    local_effects.append(check_effect)
        return local_effects

    @staticmethod
    def FindLocalEffects(message: 'Message2') -> List['Effect']:
        """The on-card effects that a message triggers, in `object_id` order.

        The list returned here decides the order `ProcessForcedEffect` resolves
        forced abilities in, the order the first player is offered the
        tie-break in, and the order the `NoSendResolve` path runs them in --
        none of which may depend on the allocator.

        Sorting by `object_id` is the key the optional path already uses
        (`ProcessOptionalEffect`). It orders by when the effect was created,
        which is what the comment on `ProcessForcedEffect` has always claimed.
        See MARVEL-31.
        """
        return sorted(EventManager.GatherLocalEffects(message),
                      key=lambda effect: effect.object_id)

    ################################################################################
    #
    def StackMessage(self, message: 'CardStateUpdatedMessage'):
        self.stack_message.append(message)

    def BroadcastStackMessage(self):
        if self.stack_message:
            # process_faces: List['CardFace'] = []
            messages = self.stack_message[:]
            self.stack_message.clear()

            for message in messages:
                # if not isinstance(message, Message.WhenCardKeywordUpdated) or \
                #     message.updated_face not in process_faces:
                self.BroadcastMessage(message)
                # process_faces.append(message.updated_face)

    def BroadcastMessage(self, message: 'Message2'):
        from game.ability.ability import TimingPriority
        from game.message import Message
        # from game.message import Message, TriggerMessage, AttackerNoneMessage, TargetsMessage, DefenderNoneMessage
        from game.message.message_type import NoSendResolve

        if self.world.is_game_over:
            from game.message.sender.sender import CanBeInstead
            if isinstance(message, CanBeInstead):
                message.SilentInstead()
            return

        # self.last_message = message

        is_playing_res = isinstance(message, Message.WhenPlayerPayingResources)

        local_effects = EventManager.FindLocalEffects(message)
        has_registered_event = self.HasRegisteredEvent(type(message))

        if not has_registered_event and not local_effects:
            return

        if CACHE_BASED_FAST_UNDO.value:
            controller_manager = message.world.controller_manager
            undo_handle = controller_manager.undo.GetFastUndoHandle(message)
            if undo_handle:
                if not undo_handle.card_ids:
                    return
                step_id = undo_handle.step_id
                Log.DebugSilent("UNDO_HANDLE", f"{step_id} {message}: {undo_handle.card_ids}")
        else:
            undo_handle = None

        if isinstance(message, NoSendResolve):
            priorities: List[TimingPriority] = [TimingPriority.Rule, TimingPriority.Constant, TimingPriority.ForcedInterrupt]

            if has_registered_event or local_effects:
                check_in_play_and_hand = False
                check_face_up_or_hand = True
                check_only_in_play = True
                checked = False

                catogories: List['EventManager.CATEGORY'] = ["Rule", "Forced"]

                if has_registered_event:
                    if isinstance(message, (
                        Message.CheckPlayerCanPayCost)):
                        check_in_play_and_hand = True
                        checked = True
                        priorities = [TimingPriority.Constant]
                        catogories = ["Forced"]
                    elif isinstance(message, (
                        Message.CheckIfAttackMessageHasKeyword)):
                        checked = True
                        priorities = [TimingPriority.Constant]
                        catogories = ["Rule"]
                    elif isinstance(message, (
                        Message.WhenRecalculateAttackDamage)):
                        checked = True
                        priorities = [TimingPriority.Constant, TimingPriority.ForcedInterrupt]
                        catogories = ["Rule", "Forced"]
                    # elif isinstance(message, (
                    #     Message.CheckEffectGeneratedResources)):
                    #     check_only_in_play_and_hand = True
                    #     priorities = [TimingPriority.Constant]
                    #     catogories = ["Rule", "Forced"]

                    # elif isinstance(message, (
                    #     Message.CheckEffectCondition)):
                    #     priorities = [TimingPriority.Rule, TimingPriority.Constant]
                    #     catogories = ["Rule", "Forced"]
                    elif isinstance(message, (
                        Message.WhenCalculateEffectCost)):
                        checked = True
                        priorities = [TimingPriority.Rule, TimingPriority.Constant]
                        catogories = ["Rule"]
                    elif isinstance(message, (
                        Message.CheckIfAllyCountLimit)):
                        checked = True
                        priorities = [TimingPriority.Constant]
                        catogories = ["Rule"]
                    elif isinstance(message, (
                        Message.CheckIfFaceIsLikeInHand)):
                        checked = True
                        priorities = [TimingPriority.Constant]
                        catogories = ["Rule"]
                    elif isinstance(message, (
                        Message.CheckIfUnitCanBeAttackBy,
                        Message.CheckIfSchemeCanBeThwartBy,
                        Message.CheckIfEffectIsIgnoreKeyWord)):
                        checked = True
                        priorities = [TimingPriority.Constant]
                        catogories = ["Rule"]
                    # else:
                    #     priorities = [TimingPriority.Rule, TimingPriority.Constant, TimingPriority.ForcedInterrupt]
                    #     catogories = ["Rule", "Forced"]

                for priority in priorities:

                    if has_registered_event:
                        for category in catogories:

                            found_global_effects = self.FindEffectsList(category, type(message), priority)

                            def check_is_in_hand(face: 'CardFace'):
                                if isinstance(message, Message.CheckIfFaceIsLikeInHand):
                                    return face.card.IsInHand()
                                return face.IsLikeInHand()

                            if found_global_effects:
                                if check_in_play_and_hand:
                                    found_global_effects = [x for x in found_global_effects if x.this.IsInPlay() or check_is_in_hand(x.this)]
                                elif check_face_up_or_hand:
                                    found_global_effects = [x for x in found_global_effects if x.this.IsFaceUp() or check_is_in_hand(x.this)]
                                elif check_only_in_play:
                                    found_global_effects = [x for x in found_global_effects if x.this.IsInPlay(is_same_face=True)]

                                if found_global_effects and not checked:
                                    name = message.name
                                    text = f"{priority}, {category}"
                                    self.debug_log.AddLog(name, text)

                                self.ProcessRuleEffect(message, found_global_effects, priority, undo_handle)

                    if local_effects:
                        found_local_effects: List['Effect'] = []

                        for check_effect in local_effects:
                            if check_effect.ability.priority == priority:
                                found_local_effects.append(check_effect)

                        if found_local_effects:
                            self.ProcessRuleEffect(message, found_local_effects, priority, undo_handle)

            return

        if local_effects and has_registered_event:
            self.debug_log_both.AddLog(message.name, f"{has_registered_event} / {len(local_effects)}")
        if local_effects and not has_registered_event:
            self.debug_log_only_local.AddLog(message.name, f"{len(local_effects)}")
        if not local_effects and has_registered_event:
            self.debug_log_only_global.AddLog(message.name, f"{has_registered_event}")

        for priority in list(TimingPriority):

            check_effects: Dict['EventManager.CATEGORY', List[Effect]] = {}

            local_effect_priority_forced: List['Effect'] = []
            local_effect_priority_optional: List['Effect'] = []

            size = 0
            if has_registered_event:
                for category in EventManager.CATEGORY_LIST:
                    found_effects = self.FindEffectsList(category, type(message), priority)
                    # check_effects[catogory] = get_only_in_play_effects(found_effects)
                    check_effects[category] = found_effects[:]
                    size += len(check_effects[category])
            if local_effects:
                for check_effect in local_effects:
                    if check_effect.ability.priority == priority:
                        if check_effect.is_forced or is_playing_res:
                            local_effect_priority_forced.append(check_effect)
                        else:
                            local_effect_priority_optional.append(check_effect)
                    size += len(local_effect_priority_forced)
                    size += len(local_effect_priority_optional)

            if size == 0:
                continue

            if "Statistics" in check_effects and check_effects["Statistics"]:
                if self.ProcessRuleEffect(message, check_effects["Statistics"], priority, undo_handle):
                    return

            if "Rule" in check_effects and check_effects["Rule"]:
                if self.ProcessRuleEffect(message, check_effects["Rule"], priority, undo_handle):
                    return

            if local_effect_priority_forced:
                if is_playing_res:
                    if self.ProcessPayingEffect(message, message.by_effect, priority):
                        return

            if local_effect_priority_forced and not is_playing_res or \
                "Forced" in check_effects and check_effects["Forced"]:
                forced_effects = local_effect_priority_forced
                if "Forced" in check_effects:
                    forced_effects += check_effects["Forced"]
                if local_effect_priority_forced:
                    if self.ProcessForcedEffect(message, local_effect_priority_forced, priority, undo_handle):
                        return

            if  local_effect_priority_optional or \
                "Optional" in check_effects and check_effects["Optional"]:
                optional_effects = local_effect_priority_optional
                if "Optional" in check_effects:
                    optional_effects += check_effects["Optional"]

                if optional_effects:
                    if self.ProcessOptionalEffect(message, optional_effects, local_effect_priority_optional, priority):
                        return
            pass

        return

