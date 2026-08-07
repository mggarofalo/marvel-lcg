from . import *

class AbilityFactoryTurnPhase:

    @staticmethod
    def WhenInYourPlayTurn(ability_type: 'AbilityType',
                           operation: OperationType[Message.WhenPlayerInTurn],
                           *,
                           conditions: ConditionsType[Message.WhenPlayerInTurn]=[],
                           ) -> 'Ability':

        return Ability(
            ability_type,
            Message.WhenPlayerInTurn,
            [
                lambda effect, message:
                    effect.initiator == message.to_player,
                *conditions
            ],
            operation)

    @staticmethod
    def WhenPlayerTurnBegin(ability_type: 'AbilityType',
                            which_player: PlayerType,
                            operation: OperationType[Message.WhenPlayerTurnBegin],
                            *,
                            conditions: ConditionsType[Message.WhenPlayerTurnBegin]=[],
                            ) -> 'Ability':

        def check_which_player(effect: 'Effect', message: 'Message.WhenPlayerTurnBegin') -> bool:
            return Condition.CheckWhichPlayer(which_player, message.to_player, effect)

        return Ability(
            ability_type,
            Message.WhenPlayerTurnBegin,
            [
                check_which_player,
                *conditions
            ],
            operation
        )

    @staticmethod
    def AfterPlayerTurnBegin(ability_type: 'AbilityType',
                            which_player: PlayerType,
                            operation: OperationType[Message.AfterPlayerTurnBegin],
                            *,
                            conditions: ConditionsType[Message.AfterPlayerTurnBegin]=[],
                            ) -> 'Ability':

        def check_which_player(effect: 'Effect', message: 'Message.AfterPlayerTurnBegin') -> bool:
            return Condition.CheckWhichPlayer(which_player, message.to_player, effect)

        return Ability(
            ability_type,
            Message.AfterPlayerTurnBegin,
            [
                check_which_player,
                *conditions
            ],
            operation
        )

    @staticmethod
    def AfterPlayerTurnEnd(ability_type: 'AbilityType',
                           which_player: PlayerType,
                           operation: OperationType[Message.AfterPlayerTurnEnd],
                           *,
                           conditions: ConditionsType[Message.AfterPlayerTurnEnd]=[],
                           ) -> 'Ability':

        def check_which_player(effect: 'Effect', message: 'Message.AfterPlayerTurnEnd') -> bool:
            return Condition.CheckWhichPlayer(which_player, message.to_player, effect)

        return Ability(
            ability_type,
            Message.AfterPlayerTurnEnd,
            [
                check_which_player,
                *conditions
            ],
            operation
        )

    @staticmethod
    def WhenPlayerTurnEnd(ability_type: 'AbilityType',
                          which_player: PlayerType,
                          operation: OperationType[Message.WhenPlayerTurnEnd],
                          *,
                          round_id: int|None=None,
                          conditions: ConditionsType[Message.WhenPlayerTurnEnd]=[],
                          ) -> 'Ability':

        def check_which_player(effect: 'Effect', message: 'Message.WhenPlayerTurnEnd') -> bool:
            return Condition.CheckWhichPlayer(which_player, message.to_player, effect)

        def check_turn_id(effect: 'Effect', message: 'Message.WhenPlayerTurnEnd') -> bool:
            if not round_id:
                return True
            return round_id == message.round_id

        return Ability(
            ability_type,
            Message.WhenPlayerTurnEnd,
            [
                check_which_player,
                check_turn_id,
                *conditions
            ],
            operation
        )

    @staticmethod
    def WhenMainSchemePhaseEnd(ability_type: 'AbilityType',
                               operation: OperationType[Message.WhenMainSchemePhaseEnd],
                               *,
                               conditions: ConditionsType[Message.WhenMainSchemePhaseEnd]=[],
                               ) -> 'Ability':


        return Ability(
            ability_type,
            Message.WhenMainSchemePhaseEnd,
            conditions,
            operation
        )

    @staticmethod
    def WhenPhaseBegin(ability_type: 'AbilityType',
                       phase_name: Literal["Player", "Villain"]|int|None,
                       operation: OperationType[Message.WhenPhaseBegin],
                       *,
                       round_id: int|None=None,
                       conditions: ConditionsType[Message.WhenPhaseBegin]=[],
                       ) -> 'Ability':

        def check_phase_name(effect: 'Effect', message: 'Message.WhenPhaseBegin') -> bool:
            if phase_name == None:
                return True
            if phase_name == "Player":
                return phase_name == message.phase_name
            if phase_name == "Villain":
                return phase_name == message.phase_name
            return phase_name == message.phase_id

        def check_round_id(effect: 'Effect', message: 'Message.WhenPhaseBegin') -> bool:
            if round_id == None:
                return True
            return round_id == message.round_id

        return Ability(
            ability_type,
            Message.WhenPhaseBegin,
            [
                check_phase_name,
                check_round_id,
                *conditions
            ],
            operation
        )

    @staticmethod
    def WhenPlayerPhaseBegin(ability_type: 'AbilityType',
                            operation: OperationType[Message.WhenPhaseBegin],
                            *,
                            conditions: ConditionsType[Message.WhenPhaseBegin]=[],
                            ) -> 'Ability':
        return AbilityFactoryTurnPhase.WhenPhaseBegin(
            ability_type,
            "Player",
            operation,
            conditions=conditions
        )

    @staticmethod
    def WhenVillainPhaseBegin(ability_type: 'AbilityType',
                            operation: OperationType[Message.WhenPhaseBegin],
                            *,
                            conditions: ConditionsType[Message.WhenPhaseBegin]=[],
                            ) -> 'Ability':
        return AbilityFactoryTurnPhase.WhenPhaseBegin(
            ability_type,
            "Villain",
            operation,
            conditions=conditions
        )

    @staticmethod
    def AfterPhaseBegin(ability_type: 'AbilityType',
                        phase_name: Literal["Player", "Villain"]|int|None,
                        operation: OperationType[Message.AfterPhaseBegin],
                        *,
                        conditions: ConditionsType[Message.AfterPhaseBegin]=[],
                        ) -> 'Ability':

        def check_phase_name(effect: 'Effect', message: 'Message.AfterPhaseBegin') -> bool:
            if phase_name == "Player":
                return phase_name == message.phase_name
            if phase_name == "Villain":
                return phase_name == message.phase_name
            if phase_name == None:
                return True
            return phase_name == message.phase_id

        return Ability(
            ability_type,
            Message.AfterPhaseBegin,
            [
                check_phase_name,
                *conditions
            ],
            operation
        )

    @staticmethod
    def AfterPlayerPhaseBegin(ability_type: 'AbilityType',
                            operation: OperationType[Message.AfterPhaseBegin],
                            *,
                            conditions: ConditionsType[Message.AfterPhaseBegin]=[],
                            ) -> 'Ability':
        return AbilityFactoryTurnPhase.AfterPhaseBegin(
            ability_type,
            "Player",
            operation,
            conditions=conditions
        )

    @staticmethod
    def WhenPhaseEnd(ability_type: 'AbilityType',
                     phase_name: Literal["Player", "Villain"]|int|None,
                     operation: OperationType[Message.WhenPhaseEnd],
                     *,
                     conditions: ConditionsType[Message.WhenPhaseEnd]=[]
                     ) -> 'Ability':

        def check_phase_name(effect: 'Effect', message: 'Message.WhenPhaseEnd') -> bool:
            if phase_name == "Player":
                return phase_name == message.phase_name
            if phase_name == "Villain":
                return phase_name == message.phase_name
            if phase_name == None:
                return True
            # if phase_name == "ThisPlayer":
            #     return effect.initiator == message.to_player
            return phase_name == message.phase_id

        return Ability(
            ability_type,
            Message.WhenPhaseEnd,
            [
                check_phase_name,
                *conditions
            ],
            operation
        )

    @staticmethod
    def WhenPlayerPhaseEnd(ability_type: 'AbilityType',
                            operation: OperationType[Message.WhenPhaseEnd],
                            *,
                            conditions: ConditionsType[Message.WhenPhaseEnd]=[]
                            ) -> 'Ability':
        return AbilityFactoryTurnPhase.WhenPhaseEnd(
            ability_type,
            "Player",
            operation,
            conditions=conditions
        )

    @staticmethod
    def WhenVillainPhaseEnd(ability_type: 'AbilityType',
                            operation: OperationType[Message.WhenPhaseEnd],
                            *,
                            conditions: ConditionsType[Message.WhenPhaseEnd]=[]
                            ) -> 'Ability':
        return AbilityFactoryTurnPhase.WhenPhaseEnd(
            ability_type,
            "Villain",
            operation,
            conditions=conditions
        )

    @staticmethod
    def WhenRoundEnd(ability_type: 'AbilityType',
                    round_id: None|int,
                    operation: OperationType[Message.WhenRoundEnd],
                    conditions: ConditionsType[Message.WhenRoundEnd]=[]
                    ) -> 'Ability':

        def check_round_id(effect: 'Effect', message: 'Message.WhenRoundEnd') -> bool:
            if round_id == None:
                return True
            return round_id == message.round_id

        return Ability(
            ability_type,
            Message.WhenRoundEnd,
            [
                check_round_id,
                *conditions
            ],
            operation
        )

    # @staticmethod
    # def WhenRoundEnd(ability_type: 'AbilityType',
    #                  operation: ActionFuncType[Send.WhenRoundEnd],
    #                  condition: ConditionType[Send.WhenRoundEnd]|None=None,
    #                  ) -> 'Ability':
    #     if condition == None:
    #         condition = lambda effect, message: True

    #     return Ability(
    #         ability_type,
    #         Send.WhenRoundEnd,
    #         lambda effect, message:
    #             condition(effect, message),
    #         operation
    #     )

    @staticmethod
    def AfterPhaseEnd(ability_type: 'AbilityType',
                      phase_name: Literal["Player", "Villain"]|int|None,
                      operation: OperationType[Message.AfterPhaseEnd],
                      *,
                      conditions: ConditionsType[Message.AfterPhaseEnd]=[]
                      ) -> 'Ability':

        def check_phase_name(effect: 'Effect', message: 'Message.AfterPhaseEnd') -> bool:
            if phase_name == "Player":
                return phase_name == message.phase_name
            if phase_name == "Villain":
                return phase_name == message.phase_name
            if phase_name == None:
                return True
            return phase_name == message.phase_id

        return Ability(
            ability_type,
            Message.AfterPhaseEnd,
            [
                check_phase_name,
                *conditions
            ],
            operation
        )

    @staticmethod
    def AfterRoundEnd(ability_type: 'AbilityType',
                      operation: OperationType[Message.AfterRoundEnd],
                      *,
                      conditions: ConditionsType[Message.AfterRoundEnd]=[],
                      ) -> 'Ability':

        return Ability(
            ability_type,
            Message.AfterRoundEnd,
            conditions,
            operation
        )

    @staticmethod
    def AfterResolveVillainPhaseStep(ability_type: 'AbilityType',
                                     step: int,
                                     operation: OperationType[Message.AfterResolveVillainPhaseStep],
                                     *,
                                     conditions: ConditionsType[Message.AfterResolveVillainPhaseStep]=[],
                                     ) -> 'Ability':

        return Ability(
            ability_type,
            Message.AfterResolveVillainPhaseStep,
            [
                lambda effect, message:
                    message.step == step,
                *conditions
            ],
            operation
        )

    @staticmethod
    def WhenVillainPhaseStepStart(ability_type: 'AbilityType',
                                  step: int,
                                  operation: OperationType[Message.WhenVillainPhaseStepStart]
                                  ) -> 'Ability':
        return Ability(
            ability_type,
            Message.WhenVillainPhaseStepStart,
            [
                lambda effect, message:
                    message.step == step
            ],
            operation
        )

