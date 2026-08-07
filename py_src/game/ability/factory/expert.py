from . import *

class AbilityFactoryExpert:

    ################################################################################
    # Expert Mode
    @staticmethod
    # CannotBeCancel only_expert_mode
    def IfExpertModeThisCannotBeCanceled() -> 'Ability':
        from game.operate.worlds import Worlds

        return Ability(
            AbilityType.NonKeyword,
            Message.CheckIfEffectCanBeCancelBy,
            [
                lambda effect, message:
                    Worlds.IsExpert(effect) and \
                    effect.this == message.check_effect.this
            ],
            lambda effect, message:
                message.SetCannotBeThwart(effect)
        )

    @staticmethod
    def IfExpertModeThisGainKeyword(*,
                                    incite: int|None=None,
                                    surge: int|None=None,
                                    hinder: int|Literal["1*"]|None=None,
                                    # toughness: int|None=None
                                    ) -> 'Ability':
        from game.operate.worlds import Worlds
        def operation(effect: 'Effect', message: 'Message.WhenCardResetKeyword') -> None:
            from game.card.face.card_type import Treachery
            from game.card.face.base import Scheme2
            diff = 1
            this = effect.this
            if incite != None:
                assert Treachery.IsType(this)
                this.GainIncite(diff * incite, effect)
            if surge != None:
                assert Treachery.IsType(this)
                this.GainSurge(diff * surge, effect)
            if hinder!= None:
                assert Scheme2.IsType(this)
                value = Worlds.ConvertPerPlayerIconToInt(hinder, effect)
                this.GainHinder(value, effect)
            # if toughness != None and Unit2.IsType(this):
            #     this.GainToughness(diff * toughness, effect)

        return Ability(
            AbilityType.NonKeyword,
            Message.WhenCardResetKeyword,
            [
                Condition2.ThisIsTrigger,
                lambda effect, message:
                    Worlds.IsExpert(effect),
            ],
            operation
        ).NoOutOfPlayLimit().NoGameAreaLimit()

    @staticmethod
    def StandardModeOnly() -> 'Ability':
        from game.card.face.card_type import Environment
        from game.ability.factory import AbilityFactory
        from game.operate.worlds import Worlds

        def formidable_foe_setup(effect: 'Effect', message: 'Message.WhenCardEnterPlay') -> None:
            this = effect.this.CastTo(Environment)
            Unused(this)

            if Worlds.IsExpert(effect):
                this.card.Flip(effect)

        return AbilityFactory.WhenCardEnterPlay(
            AbilityType.NonKeyword,
            "This",
            formidable_foe_setup
        )

    @staticmethod
    def ExpertModeOnly() -> 'Ability':
        from game.card.face.card_type import Environment
        from game.ability.factory import AbilityFactory
        from game.operate.worlds import Worlds

        def formidable_foe_setup(effect: 'Effect', message: 'Message.WhenCardEnterPlay') -> None:
            this = effect.this.CastTo(Environment)
            Unused(this)

            if not Worlds.IsExpert(effect):
                this.card.Flip(effect)

        return AbilityFactory.WhenCardEnterPlay(
            AbilityType.NonKeyword,
            "This",
            formidable_foe_setup
        )

