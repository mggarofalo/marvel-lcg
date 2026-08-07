from . import *

class AbilityFactorySetup:

    @staticmethod
    def SetupPutIntoPlay(names: List[str],
                         *,
                        #  under_control: bool=False,
                         flip_to_trait: 'CardFace.TRAITS|None'=None,
                         flip_to_name: str|None=None,
                         operation: Callable[['Effect', List['CardFace']], None]|None=None) -> 'Ability':
        from game.card.factory import CardFactory
        from game.operate.run_at import RunAt

        def kitty_pride_setup(effect: 'Effect', message: 'Message.WhenPlayerSelectHero') -> None:
            this = effect.this
            Unused(this)

            initiator = effect.GetInitiator()
            cards = CardFactory.GenerateCards(names, initiator.additional_discard_pile, effect.world)
            faces = [x.face for x in cards]

            def action():
                put_faces: List['CardFace'] = []

                for face in reversed(faces):
                    if flip_to_trait or flip_to_name:
                        face.FlipTo(effect, trait=flip_to_trait, name=flip_to_name)
                    this = face.card.face
                    if this.PutIntoPlay(initiator, effect, under_control=True):
                        put_faces.append(this)

                if operation and put_faces:
                    operation(effect, put_faces)
                from game.player.element.player_setup import HACK_HERO_ID
                if names[0].startswith(HACK_HERO_ID):
                    initiator.setup.SetupPlayerAbility(put_faces[0].card.back_faces[0])

            RunAt.WhenCardSetup(this, action)

        return AbilityFactorySetup.WhenPlayerSelectHero(
            "This",
            kitty_pride_setup
        )

    @staticmethod
    def SetupWithSetAside(names: List[str]) -> 'Ability':
        from game.card.factory import CardFactory

        def action(effect: 'Effect', message: 'Message.WhenPlayerSelectHero') -> None:
            initiator = effect.GetInitiator()
            CardFactory.GenerateCards(names, initiator.additional_discard_pile, effect.world)

        return AbilityFactorySetup.WhenPlayerSelectHero(
            "This",
            action,
        )

    @staticmethod
    def BeginGameWithSetAside(names: List[str],
                              operation: OperationType[Message.WhenPlayerSelectHero]|None=None) -> 'Ability':
        from game.card.factory import CardFactory
        from game.operate.run_at import RunAt

        def action(effect: 'Effect', message: 'Message.WhenPlayerSelectHero') -> None:
            this = effect.this
            initiator = effect.GetInitiator()
            CardFactory.GenerateCards(names, initiator.additional_discard_pile, effect.world)

            if operation:
                def action():
                    assert operation
                    operation(effect, message)
                RunAt.WhenGameBeginSetup(this, action)

        return AbilityFactorySetup.WhenPlayerSelectHero(
            "This",
            action,
        )

    @staticmethod
    def WhenPlayerSelectHero(which_hero: CardType,
                             operation: OperationType[Message.WhenPlayerSelectHero],
                            *,
                            conditions: ConditionsType[Message.WhenPlayerSelectHero]=[],
                            ) -> 'Ability':

        def check_which_hero(effect: 'Effect', message: 'Message.WhenPlayerSelectHero'):
            return Condition.CheckWhichCard(which_hero, message.trigger, effect)

        return Ability(
            AbilityType.Setup,
            Message.WhenPlayerSelectHero,
            [
                check_which_hero,
                *conditions
            ],
            operation
        )

    @staticmethod
    def WhenGameBeginSetup(operation: OperationType[Message.WhenGameBeginSetup],
                           *,
                           conditions: ConditionsType[Message.WhenGameBeginSetup]=[],
                           ) -> 'Ability':

        return Ability(
            AbilityType.Setup,
            Message.WhenGameBeginSetup,
            conditions,
            operation
        )

    @staticmethod
    def SetModularSetsAside(max_sets_num: int|Callable[['Effect'], int]) -> 'Ability':
        def when_game_begin(effect: 'Effect', message: 'Message.WhenGameBeginSetup') -> None:
            encounter_sets: List[str] = []
            standard_encounter_sets: List[str] = []
            for encounter_set in message.encounter_set_names:
                if encounter_set.startswith("standard") or \
                    encounter_set.startswith("expert"):
                    standard_encounter_sets.append(encounter_set)
                else:
                    encounter_sets.append(encounter_set)

            if isinstance(max_sets_num, int):
                check_num = max_sets_num
            else:
                check_num = max_sets_num(effect)
            if max_sets_num:
                assert len(encounter_sets) >= check_num, f"You need {check_num} encounter sets to play this scenario"
                encounter_sets = encounter_sets[:check_num]
            message.encounter_set_names = standard_encounter_sets

            from game.card.factory import CardFactory
            card_ids: List[str] = []
            for encounter_set in encounter_sets:
                card_ids += CardFactory.LoadEncounterSet(encounter_set)
            assert message.set_aside_modular_card_ids == []
            message.set_aside_modular_card_ids = card_ids

        return AbilityFactorySetup.WhenGameBeginSetup(
            when_game_begin
        )

    @staticmethod
    def WhenGameInitialize(operation: OperationType[Message.WhenGameInitialize],
                           *,
                           conditions: ConditionsType[Message.WhenGameInitialize]=[],
                           ) -> 'Ability':

        return Ability(
            AbilityType.Setup,
            Message.WhenGameInitialize,
            conditions,
            operation
        )

    @staticmethod
    def WhenGameWouldBegin(operation: OperationType[Message.WhenGameWouldBegin],
                           *,
                           conditions: ConditionsType[Message.WhenGameWouldBegin]=[],
                           ) -> 'Ability':

        return Ability(
            AbilityType.Setup,
            Message.WhenGameWouldBegin,
            conditions,
            operation
        )

    @staticmethod
    def WhenCardSetup(which_card: 'Literal["This"]|CardFace',
                      operation: OperationType[Message.WhenCardSetup]) -> 'Ability':

        def check_who(effect: 'Effect', message: 'Message.WhenCardSetup') -> bool:
            if which_card == "This":
                return Condition2.ThisIsTrigger(effect, message)
            else:
                return which_card == message.trigger

        def is_before_put_setup_cards_into_play(effect: 'Effect', message: 'Message.WhenCardSetup') -> bool:
            return not message.before_put_setup_cards

        return Ability(
            AbilityType.Setup,
            Message.WhenCardSetup,
            [
                check_who,
                is_before_put_setup_cards_into_play
            ],
            operation,
            is_local=which_card == "This"
        )

    @staticmethod
    def WhenCardSetupBeforePutSetupCards(operation: OperationType[Message.WhenCardSetup]) -> 'Ability':
        def check_who(effect: 'Effect', message: 'Message.WhenCardSetup') -> bool:
            return effect.this == message.trigger

        def is_before_put_setup_cards(effect: 'Effect', message: 'Message.WhenCardSetup') -> bool:
            return message.before_put_setup_cards

        return Ability(
            AbilityType.Setup,
            Message.WhenCardSetup,
            [
                check_who,
                is_before_put_setup_cards
            ],
            operation
        )

    @staticmethod
    def AfterGameSetup(operation: OperationType[Message.WhenGameWouldBegin]) -> 'Ability':
        return AbilityFactorySetup.WhenGameWouldBegin(operation)

