from . import *

class AbilityFactoryForm:

    # @staticmethod
    # def AfterIdentityChangeFormEnd(ability_type: 'AbilityType',
    #                             operation: OperationType[Send.AfterIdentityChangeFormEnd],
    #                             *,
    #                             condition: ConditionType[Send.AfterIdentityChangeFormEnd]|None=None,
    #                             ) -> 'Ability':
    #     if condition == None:
    #         condition = [ True

    #     return Ability(
    #         ability_type,
    #         Send.AfterIdentityChangeFormEnd,
    #         condition,
    #         operation,
    #     )

    @staticmethod
    def AfterUnitChangeFormInternal(ability_type: 'AbilityType',
                                which_unit: CardType|Literal["You"],
                                operation: OperationType[Message.AfterUnitChangeForm],
                                *,
                                to_this_form: Literal[True]|None=None,
                                to_form: CardType=None,
                                # to_trait: "CardFace.TRAITS|None"=None,
                                to_this_additional_form: bool|None=None,
                                is_change_additional_form: bool|None=None,
                                conditions: ConditionsType[Message.AfterUnitChangeForm]=[],
                                ) -> 'Ability':

        def check_which_unit(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> bool:
            rule = Condition.GetYouRule(which_unit, identity=True)
            return Condition.CheckWhichCard(rule, message.trigger, effect)
            # if which_unit == "Attached":
            #     return effect.this.GetBindFace() == message.trigger
            # Is you, 12016
            # effect.GetInitiator().GetIdentity() == message.trigger

        def check_to_this_form(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> bool:
            if to_this_form == None:
                return True
            return effect.this == message.trigger and message.to_additional_form == None

        def check_to_this_additional_form(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> bool:
            if to_this_additional_form == None:
                return True
            return effect.this == message.to_additional_form

        def check_is_change_additional_form(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> bool:
            if is_change_additional_form == None:
                return True
            return is_change_additional_form == (message.to_additional_form != None)

        def check_to_form(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> bool:
            if to_form == None:
                return True
            # Fix "04093" "21001b"
            if Condition.CheckWhichCard(to_form, message.would_change_message.from_form, effect):
                return False
            return Condition.CheckWhichCard(to_form, message.trigger.GetControlByPlayer().GetIdentity(), effect)

        return Ability(
            ability_type,
            Message.AfterUnitChangeForm,
            [
                check_which_unit,
                check_to_this_form,
                check_to_this_additional_form,
                check_to_form,
                check_is_change_additional_form,
                *conditions
            ],
            operation,
            is_local=True if to_this_form == True else None
        )

    @staticmethod
    def AfterUnitChangeForm(ability_type: 'AbilityType',
                            which_unit: CardType|Literal["You"],
                            operation: OperationType[Message.AfterUnitChangeForm],
                            *,
                            to_form: 'CardType'=None,
                            to_this_form: Literal[True]|None=None,
                            to_this_additional_form: bool|None=None,
                            is_change_additional_form: bool|None=None, # include mass form
                            # is_change_mass_form: bool|None=None,
                            from_form: 'CardType'=None,
                            conditions: ConditionsType[Message.AfterUnitChangeForm]=[],
                            ) -> 'Ability':

        def action(effect: 'Effect', message: 'Message.WhenUnitWouldChangeForm'):
            this = effect.this
            ability = AbilityFactoryForm.AfterUnitChangeFormInternal(
                ability_type,
                which_unit,
                operation,
                to_this_form=to_this_form,
                to_form=to_form,
                to_this_additional_form=to_this_additional_form,
                is_change_additional_form=is_change_additional_form,
                conditions=conditions,
            )
            ability.CopyFromDelayEffect(effect)
            this.effect.RegisterTemp(
                ability,
                unregister_after_exec=False,
                until_event_end=message
            )
            pass

        if from_form != None:
            return AbilityFactoryForm.WhenUnitWouldChangeForm(
                AbilityType.DelayAbility,
                which_unit,
                action,
                from_form=from_form,
                to_form=to_form,
            )
        else:
            return AbilityFactoryForm.AfterUnitChangeFormInternal(
                ability_type,
                which_unit,
                operation=operation,
                to_this_form=to_this_form,
                to_form=to_form,
                # to_trait=to_trait,
                to_this_additional_form=to_this_additional_form,
                is_change_additional_form=is_change_additional_form,
                conditions=conditions,
            )

    @staticmethod
    def WhenUnitWouldChangeForm(ability_type: 'AbilityType',
                                which_unit: CardType|Literal["You"],
                                operation: OperationType[Message.WhenUnitWouldChangeForm],
                                *,
                                to_form: CardType=None,
                                from_form: CardType=None,
                                during_your_turn: bool|None=None,
                                conditions: ConditionsType[Message.WhenUnitWouldChangeForm]=[],
                                ) -> 'Ability':

        def check_from_form(effect: 'Effect', message: 'Message.WhenUnitWouldChangeForm') -> bool:
            return Condition.CheckWhichCard(from_form, message.trigger, effect)

        def check_which_unit(effect: 'Effect', message: 'Message.WhenUnitWouldChangeForm') -> bool:
            rule = Condition.GetYouRule(which_unit, identity=True)
            return Condition.CheckWhichCard(rule, message.trigger, effect)
            # if which_unit == "Attached":
            #     return effect.this.GetBindFace() == message.trigger
            # Is you, 12016
            # effect.GetInitiator().GetIdentity() == message.trigger

        def check_to_hero_form(effect: 'Effect', message: 'Message.WhenUnitWouldChangeForm') -> bool:
            if to_form == None:
                return True
            # Fix "04093" "21001b"
            if Condition.CheckWhichCard(to_form, message.from_form, effect):
                return False
            return Condition.CheckWhichCard(to_form, message.to_form, effect)

        def check_during_your_turn(effect: 'Effect', message: 'Message.WhenUnitWouldChangeForm') -> bool:
            if during_your_turn == None:
                return True
            return effect.world.phase.IsPlayerPhase(message.trigger.GetControlByPlayer())

        # def check_is_change_additional_form(effect: 'Effect', message: 'Send.WhenIdentityWouldChangeForm') -> bool:
        #     if is_change_additional_form == None:
        #         return True
        #     return is_change_additional_form == (message.to_additional_form != None)

        return Ability(
            ability_type,
            Message.WhenUnitWouldChangeForm,
            [
                check_from_form,
                check_which_unit,
                check_to_hero_form,
                check_during_your_turn,
                *conditions
            ],
            operation,
            is_local=which_unit == "This"
        )
