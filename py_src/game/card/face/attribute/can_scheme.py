from . import *

@dataclass
class SchemeProperty(PowerProperty):
    do_not_give_boost: bool = field(default=False)
    against_player: 'Player|None' = field(default=None)
    would_act_message: 'Message.WhenEnemyWouldActivate|None' = field(default=None)
    give_additional_boost: int = field(default=0)

    def GetScheme(self, this: 'CardFace') -> int:
        value = 0
        if self.is_basic_power:
            if HasScheme.IsType(this):
                value = this.scheme
        return self.additional_value + value

class HasScheme(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        self.base_scheme = 0
        self.printed_scheme = 0
        # Every live "has a base SCH of N" grant on this face, oldest first, one
        # entry per granting `Effect`. `base_scheme` is the last entry's value,
        # or `printed_scheme` when there is none. See `PushBaseScheme`.
        self.base_scheme_grants: List[Tuple['Effect', int]] = []

        super().__init__(paper)

        self.RegisterAttribute("SCH", "printed_scheme")
        self.RegisterInfoDict('scheme')

    @override
    def OnResetKeywords(self, by_effect: 'Effect'):
        # `ResetKeywords` throws away every keyword contribution from every
        # source, so the record of who granted a base has to go with them --
        # otherwise a grant that no longer contributes anything would still be
        # picked as the value to fall back to. See MARVEL-111.
        self.base_scheme_grants = []
        self.base_scheme = self.printed_scheme
        self.GainScheme(self.base_scheme, by_effect)
        return super().OnResetKeywords(by_effect)

    @final
    def PushBaseScheme(self, value: int, by_effect: 'Effect') -> None:
        """Add `by_effect`'s base-SCH grant. The newest live grant is the base.

        Two sources can grant a base statistic to one character at once, and the
        one applied last is the one that counts -- but the earlier one is still
        active underneath, so when the newest ends the character falls back to it
        rather than to its printed line. That needs the whole ordered set kept,
        not just the value currently showing. See MARVEL-111.

        A source that grants again while already on the stack moves to the top,
        because that is a fresh application.
        """
        self.base_scheme_grants = [
            grant for grant in self.base_scheme_grants if grant[0] is not by_effect]
        self.base_scheme_grants.append((by_effect, value))
        self.SetBaseScheme(value, by_effect)

    @final
    def PopBaseScheme(self, by_effect: 'Effect') -> None:
        """Drop `by_effect`'s grant -- *its own* entry, not the newest one.

        The out-of-order case is the whole point: the first of two grants ending
        must leave the second one showing, and dropping the top entry instead
        would put the character back on the printed line with a grant still live.
        """
        self.base_scheme_grants = [
            grant for grant in self.base_scheme_grants if grant[0] is not by_effect]
        if self.base_scheme_grants:
            value = self.base_scheme_grants[-1][1]
        else:
            value = self.printed_scheme
        self.SetBaseScheme(value, by_effect)

    @final
    def SetBaseScheme(self, value: int, by_effect: 'Effect'):
        if self.base_scheme != value:
            diff = value - self.base_scheme
            self.base_scheme = value
            self.GainScheme(diff, by_effect)

    @final
    def GainScheme(self, diff: int, by_effect: 'Effect', *, render_ui: bool=False):
        self.GainKeyword(diff, 'SCH', by_effect, render_ui=render_ui)

    @property
    def scheme(self) -> int:
        return max(0, self.GetKeyword('SCH'))

################################################################################
#
class CanScheme(CardFace):
    
    def SchemesInternal(self, by_effect: 'Effect', property: SchemeProperty) -> 'Message.AfterUnitSchemeEnd|None':
        from game.message import Message
        from game.card.face.attribute.can_boost import CanBoost
        from game.card.face.base import Unit2
        from game.card.face.base import Enemy
        from game.effect.rule import GameRule, SchemeActive
        from game.operate.worlds import Worlds

        is_base_scheme = property.is_basic_power

        this = self.card.CastTo(Unit2)
        assert Unit2.IsType(this), f"{this=}"

        target_scheme = None
        if Enemy.IsType(this):
            check_scheme_message = Message.GettingEnemySchemeTarget(this, by_effect)
            check_scheme_message.Send()
            if check_scheme_message.target_scheme:
                target_scheme = check_scheme_message.target_scheme

        would_sch_message = Message.WhenUnitWouldScheme(this, by_effect, property=property)
        would_sch_message.Send()
        if would_sch_message.is_be_instead:
            return None

        if Enemy.IsType(this):
            activate_message = Message.WhenEnemyActivateAgainstYou(this, property.against_player, by_effect, would_sch_message, property.would_act_message)
            activate_message.Send()
        else:
            activate_message = None

        if is_base_scheme:
            unit = this.card.CastTo(Unit2)
            use_basic_power_message = Message.WhenUnitUseBasicPower(unit, "SCH", would_sch_message)
            use_basic_power_message.Send()
        else:
            use_basic_power_message = None

        if is_base_scheme and CanBoost.IsType(this) and not property.do_not_give_boost:
            boost_size = this.GetBoostCardNum(would_sch_message) + property.give_additional_boost
            this.GiveFacedownBoostCardsInternal(boost_size, GameRule(this), would_sch_message)

        main_scheme = Worlds.FindMainScheme(self)
        if not main_scheme:
            return None

        boost_cards: List['CardFace'] = []
        if is_base_scheme:
            # assert property.base_value == this.scheme
            being_sch_message = Message.WhenSchemeBeingScheme(main_scheme, would_sch_message)
            being_sch_message.Send()

            def gain_sch(face: CardFace, boost_value: int) -> None:
                nonlocal boost_cards
                boost_cards.append(face)
                this.card.CastTo(Unit2).GainForThisActive(
                    GameRule(face),
                    would_sch_message,
                    scheme=boost_value,
                    render_ui=True
                )

            if CanBoost.IsType(this):
                this.ResolveBoostCards(being_sch_message, gain_sch)
                if Worlds.IsGameOver(by_effect):
                    return None

        this = this.CastTo(Unit2)
        value = property.GetScheme(this)

        if not target_scheme:
            target_scheme = Worlds.FindMainScheme(self)
        if not target_scheme:
            return None

        recalculate = Message.WhenRecalculateSchemeValue(this, target_scheme, value, would_sch_message)
        recalculate.Send()
        value = recalculate.value

        if would_sch_message.remove_threat_instead_of_placing:
            target_scheme.RemoveThreatInternal(this, value, would_sch_message.remove_threat_instead_of_placing)
            value *= - 1
        else:
            target_scheme.PlaceThreatInternal(value, SchemeActive(this), would_sch_message)
        # this.PlaceThreatOnSchemes([scheme], value, by_effect) # if we call this, the UI is strange with "01186"
        end_message = Message.AfterUnitSchemeEnd(this, value, target_scheme, boost_cards, by_effect, [would_sch_message])
        end_message.Send()

        if activate_message:
            assert Enemy.IsType(this)
            after_activate_message = Message.AfterEnemyActivationEnd(this, end_message, activate_message)
            after_activate_message.Send()

        if use_basic_power_message and not this.IsDefeated():
            unit = this.card.CastTo(Unit2)
            after_use_basic_power_message = Message.AfterUnitUseBasicPower(unit, "SCH", end_message, use_basic_power_message)
            after_use_basic_power_message.Send()
        return end_message

    def BasicSchemes(self, by_effect: 'Effect', *, property: 'SchemeProperty|None' = None) -> 'Message.AfterUnitSchemeEnd|None':
        if not self.IsCanScheme():
            return None
        if property == None:
            property = SchemeProperty(is_basic_power=True)
        else:
            property.is_basic_power = True
        message = self.SchemesInternal(by_effect, property)
        return message

    def IsCanScheme(self) -> bool:
        return self.IsInPlay()

