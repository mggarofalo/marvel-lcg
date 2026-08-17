from . import *

class HasHealth(HasAttribute, CanAttacked):

    @override
    def __init__(self, paper: 'Paper') -> None:
        self.printed_health = -1
        self.printed_health_numeral = -1
        self.starting_health = -1

        # 16-mc16_galaxys_most_wanted_rules_website-compressed.pdf
        # A character with infinite hit points cannot be defeated by
        # taking damage. However, damage may still be dealt to
        # that character through attacks and card abilities.

        # Some characters may have an infinite number of hit points.
        # A character with infinite hit points cannot be defeated by
        # taking damage, as the amount of damage that character
        # takes will never cause its remaining hit points to reach zero.
        # However, damage may still be dealt to a character with
        # infinite hit points through attacks and card abilities.
        self.is_infinite_health: bool = False

        self.considered_at_least_hp_dict: Dict['Effect', int] = {}

        self.base_health = 0
        # Every live "has a base hit points of N" grant on this face, oldest
        # first, one entry per granting `Effect`. `base_health` is the last
        # entry's value, or `printed_health` when there is none. See
        # `PushBaseHealth`.
        self.base_health_grants: List[Tuple['Effect', int]] = []
        # self.health_max = 0

        super().__init__(paper)

        def parse(value: str):
            if value == "INF":
                self.is_infinite_health = True
                self.printed_health = 0
                self.printed_health_numeral = 0
            else:
                self.printed_health = self.FormatPlayerNumValue(value)
                digit = ''.join(ch for ch in value if ch.isdigit())
                self.printed_health_numeral = int(digit)
            self.starting_health = self.printed_health
        self.RegisterAttribute("HP", parse)
        self.RegisterInfoDict('health')
        self.RegisterInfoDict('is_infinite_health')

    @override
    def CheckPrintedValue(self):
        if not self.is_infinite_health:
            assert self.printed_health >= 0, f"{self.paper.card_id=}"
        return super().CheckPrintedValue()

    @final
    @property
    def health(self) -> int:
        def get_health():
            if self.is_infinite_health:
                return 1 # if we set it 0, it cannot be attacked
            else:
                return self.components.health.GetHealth()
        health = get_health()
        considered_at_least_hp = self.considered_at_least_hp
        if considered_at_least_hp and \
            health < considered_at_least_hp:
            return considered_at_least_hp
        else:
            return health

    @final
    @property
    def considered_at_least_hp(self) -> int:
        health = 0
        for effect in self.considered_at_least_hp_dict:
            if health < self.considered_at_least_hp_dict[effect]:
                health = self.considered_at_least_hp_dict[effect]
        return health

    @final
    @property
    def max_health(self) -> int:
        if self.is_infinite_health:
            return 0
        else:
            return self.components.health.GetMaxHealth()

    @final
    @property
    def sustained(self) -> int:
        if self.is_infinite_health:
            return 0
        else:
            return self.max_health - self.health

    @final
    @property
    def is_full_heath(self) -> bool:
        if self.is_infinite_health:
            return True
        return self.sustained == 0

class CanHealth(HasHealth):

    @override
    def OnFlip(self, by_effect: 'Effect', origin_face: 'CardFace|None'):
        from game.effect.rule import ResetKeyword
        from game.card.face.base import Unit2
        # The record of who granted a base goes the same way as
        # `considered_at_least_hp_dict` right beside it, and for the same reason:
        # this face is being put back on its printed line wholesale, so a grant
        # left on the stack would be a value to fall back to that no longer
        # corresponds to anything. See MARVEL-111.
        self.base_health_grants = []
        self.base_health = self.printed_health
        self.considered_at_least_hp_dict = {}
        # self.health_max = self.base_health
        if origin_face and not Unit2.IsType(origin_face) or \
            origin_face and origin_face.is_infinite_health:
            self.ResetHealth(ResetKeyword(self))
        return super().OnFlip(by_effect, origin_face)

    @override
    def OnReset(self, message: 'Message.WhenCardReset_Text') -> None:
        self.components.health.SetHealth(0)
        self.components.health.SetMaxHealth(0)
        return super().OnReset(message)

    ################################################################################
    #
    @final
    def ResetHealth(self, by_effect: 'Effect', num: int|Literal["Max", "Printed", "10*"]="Max"):
        from game.card.face.base import Unit2
        from game.operate.worlds import Worlds

        # As in `OnFlip`: the hit-point line is being rebuilt from printed, so
        # the live base grants go with it. See MARVEL-111.
        self.base_health_grants = []
        self.base_health = self.printed_health
        self.components.health.SetMaxHealth(self.base_health)
        self.considered_at_least_hp_dict = {}

        if not isinstance(num, int):
            assert self.card.face == self

        # self.health_max = self.base_health
        if num == "Printed":
            health_max = self.printed_health
        elif num == "Max":
            health_max = self.max_health
        else:
            health_max = Worlds.ConvertPerPlayerIconToInt(num, by_effect)
        self.components.health.SetHealth(health_max)

        assert Unit2.IsType(self)
        reset_message = Message.AfterUnitHitPointReset(self, by_effect)
        reset_message.Send()

    @final
    def MoveDamage(self, damage: int, target: 'Unit2', by_effect: 'Effect'):
        # Q: Does moving damage discard tough status cards from
        # the target enemy?
        # A: Yes. If damage is moved to a character, the moved
        # damage is considered to be dealt to that character
        lost_heath = self.GetLostHealth()
        if lost_heath < damage:
            damage = lost_heath
        if damage != 0:
            self.HealHealth(damage, by_effect)
            if by_effect.ability.IsLabel('attack'):
                # Fix "01049" interaction with retaliate
                self.DealDamage([target], damage, by_effect)
            else:
                target.TakeDamage(self, damage, by_effect)

    @final
    def HealHealth(self, value: int|Literal["All", "Printed"], by_effect: 'Effect') -> 'Message.AfterUnitHealHealth|None':
        if not self.IsInPlay():
            return None

        if value == "All":
            value = self.sustained
        elif value == "Printed":
            value = self.printed_health - self.health

        message = self.GainOnlyHealth(value, by_effect)

        if isinstance(message, Message.AfterUnitHealHealth):
            return message
        else:
            return None

    @final
    def TakeIndirectDamage(self, source: 'CardFace', damage: int, by_effect: 'Effect', *, from_atk_message: 'Message.WhenUnitWouldAttack|None'=None, as_group: bool=False, operation: Callable[['Message.AfterUnitTookDamage'], None]|None=None):
        from game.operate.worlds import Worlds

        player = self.GetControlByPlayer()
        if as_group:
            units = Worlds.GetOnFieldFriendlyCharacters(by_effect)
        else:
            units = [player.GetIdentity()] + player.GetControlAllies()
        return player.AssignDamage(units, source, damage, by_effect, operation=operation, from_atk_message=from_atk_message)

    @override
    def TakeDamage(self, source: 'CardFace', property: 'int|DamageProperty', by_effect: 'Effect',
                    would_attack_unit_message: 'Message.WhenUnitWouldAttackUnit|None'=None,
                    *,
                    from_atk_message: 'Message.WhenUnitWouldAttack|None'=None, # Fix for indirect damage, "43036"
                    operation: Callable[['Message.AfterUnitTookDamage'], None]|None=None,
                    ) -> 'Message.AfterUnitDefeatedUnit|Message.AfterUnitTookDamage|None':
        messages = self.TakeDamageWithOverkillTarget(source, property, by_effect, would_attack_unit_message, from_atk_message=from_atk_message, operation=operation)
        if messages:
            return messages[0]
        return None

    @override
    # [0]: current
    # [1]: overkill target
    def TakeDamageWithOverkillTarget(self, source: 'CardFace', property: 'int|DamageProperty',
                                    by_effect: 'Effect',
                                    would_attack_unit_message: 'Message.WhenUnitWouldAttackUnit|None'=None,
                                    overkill_target: 'Unit2|None'=None,
                                    *,
                                    from_atk_message: 'Message.WhenUnitWouldAttack|None'=None, # Fix for indirect damage, "43036"
                                    operation: Callable[['Message.AfterUnitTookDamage'], None]|None=None
                                    ) -> List['Message.AfterUnitDefeatedUnit|Message.AfterUnitTookDamage']:
        from game.card.face.base import Unit2

        return_messages: List['Message.AfterUnitDefeatedUnit|Message.AfterUnitTookDamage'] = []

        lost_message_overkill_helper = None
        lost_message_helper = self.TakeDamageNoDeath(source, property, by_effect, would_attack_unit_message, from_atk_message=from_atk_message, operation=operation)
        excess_damage = 0

        if lost_message_helper != None:

            # if isinstance(lost_message, int):
            #     message = Message.AfterUnitTookDamage(self, source, 0, by_effect, would_take_damage_message, excess_damage=excess_damage)
            #     message.Send()
            #     if operation:
            #         operation(message)
            #     return message

            if lost_message_helper.lost_message:
                total_took_damage = lost_message_helper.lost_message.value
            else:
                total_took_damage = 0

            excess_damage = lost_message_helper.excess_damage

            if overkill_target and excess_damage:
                lost_message_overkill_helper = overkill_target.TakeDamageNoDeath(source, DamageProperty(damage=excess_damage, is_from_overkill=True), by_effect, would_attack_unit_message, from_atk_message=from_atk_message)

            units: List['Unit2'] = []
            if self.IsInPlay() and Unit2.IsType(self):
                units.append(self)
            if overkill_target and overkill_target.IsInPlay() and excess_damage:
                units.append(overkill_target)

            world = self.card.world
            first_player = world.GetFirstPlayer()

            while units:
                if not units:
                    break

                sorted_units = first_player.ChooseOrder(
                    units,
                    prompt="Simultaneous Overkill",
                )
                unit = sorted_units[0]

                if unit:
                    if lost_message_overkill_helper and \
                        lost_message_overkill_helper.lost_message and \
                        lost_message_overkill_helper.lost_message.trigger == unit:
                        would_take_damage_message = lost_message_overkill_helper.would_take_damage_message
                    else:
                        would_take_damage_message = lost_message_helper.would_take_damage_message

                    units.remove(unit)

                    if unit.health <= 0 and \
                        unit.Death(source, by_effect, would_attack_unit_message):
                        if would_attack_unit_message:
                            would_attack_unit_message.would_atk_message.has_defeated_target = True

                        # Fix defeating "27073" while "27081" is in play
                        if unit.IsInPlay():
                            took_damage_message = Message.AfterUnitTookDamage(
                                unit,
                                source,
                                total_took_damage - excess_damage,
                                by_effect,
                                would_take_damage_message,
                                excess_damage=excess_damage)
                            took_damage_message.Send()
                            if operation:
                                operation(took_damage_message)

                        message = Message.AfterUnitDefeatedUnit(
                            source,
                            unit,
                            total_took_damage,
                            excess_damage,
                            overkill_target,
                            by_effect,
                            would_take_damage_message)
                        message.Send()

                        return_messages.append(message)
                    else:
                        if not overkill_target:
                            damage = total_took_damage
                        elif unit == overkill_target:
                            damage = excess_damage
                        else:
                            damage = total_took_damage - excess_damage
                        took_damage_message = Message.AfterUnitTookDamage(
                            unit,
                            source,
                            damage,
                            by_effect,
                            would_take_damage_message,
                            excess_damage=0)
                        took_damage_message.Send()
                        if operation:
                            operation(took_damage_message)
                        return_messages.append(took_damage_message)

        if lost_message_helper and lost_message_helper.would_take_damage_message.would_deal_damage_message.damage > 0:
            after_damage_message = Message.AfterFaceDealDamage(
                lost_message_helper.would_take_damage_message.would_deal_damage_message,
                return_messages,
                by_effect)
            after_damage_message.Send()

        return return_messages

    @final
    def TakeDamageNoDeath(self, source: 'CardFace', property: 'int|DamageProperty', by_effect: 'Effect',
                    would_attack_unit_message: 'Message.WhenUnitWouldAttackUnit|None'=None,
                    *,
                    from_atk_message: 'Message.WhenUnitWouldAttack|None'=None, # Fix for indirect damage, "43036"
                    operation: Callable[['Message.AfterUnitTookDamage'], None]|None=None,
                    ) -> 'Message.AfterUnitLossHealthHelper|None':
        from game.card.face.base import Unit2
        # When player use event card, this == event card, but source == hero
        assert Unit2.IsType(self)
        this = self

        if this.IsDefeated():
            return None

        if isinstance(property, int):
            property = DamageProperty(property)

        if from_atk_message == None and would_attack_unit_message != None:
            from_atk_message = would_attack_unit_message.would_atk_message

        would_deal_damage_message = Message.WhenFaceWouldDealDamage(source, this, property, by_effect, would_attack_unit_message, from_atk_message)
        would_deal_damage_message.Send()
        assert property.damage == would_deal_damage_message.damage

        if would_deal_damage_message.is_be_instead:
            return None

        if property.damage <= 0:
            Message.WhenDamageIsZero_Text(this, source, True)
            return None

        # would_atk_message = being_atk_message.would_atk_message if being_atk_message else None

        # if not isinstance(being_atk_message, Send.WhenUnitBeingAttack):
        #     being_atk_message = None

        would_take_damage_message = Message.WhenUnitWouldTakeDamage(this, source, property, by_effect, would_deal_damage_message)
        would_take_damage_message.Send()
        if would_take_damage_message.is_be_instead:
            return None
        if would_attack_unit_message and not source.IsInPlay():
            return None
        value = would_take_damage_message.will_take_damage

        if value == 0 or would_take_damage_message.IsBePrevent():
            # Overkill damage is dealt simultaneously with the attack’s standard damage, and Nightcrawler can only prevent damage to one character.
            # if would_take_damage_message.be_prevent:
            #     excess_damage = 0
            # else:
            excess_damage = would_take_damage_message.overkill_damage
            return Message.AfterUnitLossHealthHelper(
                None, would_take_damage_message, excess_damage
            )

        if not would_take_damage_message.IsBePrevent():
            excess_damage = 0

            assert this.IsInPlay()

            max_sustained = would_take_damage_message.cannot_have_more_than_sustained

            damage_message = this.UpdateHealth(-1 * value, by_effect, max_sustained=max_sustained)
            if damage_message:
                if this.health < 0:
                    deal_message = Message.WhenCalcDealExcessDamage(
                        source,
                        this,
                        -1 * this.health,
                        by_effect,
                        would_attack_unit_message)
                    deal_message.Send()
                    excess_damage = deal_message.excess_damage

            if isinstance(damage_message, Message.AfterUnitLossHealth):
                # damage_message.excess_damage = excess_damage
                # damage_message.would_take_damage_message = would_take_damage_message
                return Message.AfterUnitLossHealthHelper(
                    damage_message, would_take_damage_message, excess_damage
                )
            else:
                return None
        return None

    @final
    def CanHeath(self) -> bool:
        return self.GetLostHealth() > 0

    @final
    def GetLostHealth(self) -> int:
        return self.max_health - self.health

    # def IsDeath(self) -> bool:
    #     return self.IsDefeated()

    @override
    def IsDefeated(self) -> bool:
        return self.health <= 0 or super().IsDefeated()

    ################################################################################
    #
    @final
    def SetHealth(self, value: int, by_effect: 'Effect') -> None:
        from game.card.face.base import Unit2
        # Send.WhenCardWouldUpdateKeyword_Text(self, value, 'HP', by_effect)
        self.components.health.SetHealth(value)
        if not self.is_infinite_health:
            assert self.health <= self.max_health, f"{self.health=} {self.max_health=}"
        self.LimitHealth(by_effect)

        if Unit2.IsType(self): # Fix "ultron_facedown_drone"
            # Send.WhenCardKeywordUpdated(self, value, 'HP', by_effect)
            message = Message.AfterUnitHitPointSet(self, by_effect)
            message.Send()

    @final
    def GainHealth(self, value: int, by_effect: 'Effect') -> None:
        if self.card.state.is_flipping:
            self.GainOnlyMaxHealth(value, by_effect)
        else:
            self.GainHealthAndMaxHealth(value, by_effect)

    @final
    def GainOnlyMaxHealth(self, value: int, by_effect: 'Effect') -> None:
        # Send.WhenCardWouldUpdateKeyword_Text(self, value, 'MAXHP', by_effect)
        self.UpdateMaxHealth(value, by_effect)
        # Send.WhenCardKeywordUpdated(self, value, 'MAXHP', by_effect)
        # if check_limit:
        #     self.LimitHealth(by_effect)

    @final
    def GainOnlyHealth(self, value: int, by_effect: 'Effect') -> 'Message.AfterUnitHealHealth|Message.AfterUnitLossHealth|None':
        # Send.WhenCardWouldUpdateKeyword_Text(self, value, 'HP', by_effect)
        message = self.UpdateHealth(value, by_effect)
        # if value > 0:
        #     gain_value = diff
        # else:
        #     gain_value = -1 * diff
        # Send.WhenCardKeywordUpdated(self, gain_value, 'HP', by_effect)
        # This line will do `Death` first when deal damage
        # And ultron face down drone will back to a normal player card
        self.LimitHealth(by_effect)
        return message

    @final
    def GainHealthAndMaxHealth(self, value: int, by_effect: 'Effect') -> None:
        if value > 0:
            self.GainOnlyMaxHealth(value, by_effect)
            self.GainOnlyHealth(value, by_effect)
        else:
            self.GainOnlyHealth(value, by_effect)
            self.GainOnlyMaxHealth(value, by_effect)

    @final
    def PushBaseHealth(self, value: int, by_effect: 'Effect') -> None:
        """Add `by_effect`'s base hit-point grant. The newest live grant is the base.

        Two sources can grant a base statistic to one character at once, and the
        one applied last is the one that counts -- but the earlier one is still
        active underneath, so when the newest ends the character falls back to it
        rather than to its printed line. That needs the whole ordered set kept,
        not just the value currently showing. See MARVEL-111.

        Hit points are the statistic where the ordering is visible rather than
        bookkeeping: `SetBaseHealth` moves current health along with max, so
        falling back to the wrong value is a character taking or losing damage,
        and falling back to a printed 0 is a character dying.

        A source that grants again while already on the stack moves to the top,
        because that is a fresh application.
        """
        self.base_health_grants = [
            grant for grant in self.base_health_grants if grant[0] is not by_effect]
        self.base_health_grants.append((by_effect, value))
        self.SetBaseHealth(value, by_effect)

    @final
    def PopBaseHealth(self, by_effect: 'Effect') -> None:
        """Drop `by_effect`'s grant -- *its own* entry, not the newest one.

        The out-of-order case is the whole point, and on hit points it is fatal:
        a Drone Minion holding grants from two copies of Ultron Drones (`01140`,
        reprinted as `26031`) prints 0 hit points, so putting it back on its
        printed line while the second grant is still live kills it. Dropping the
        top entry instead of this effect's own would do exactly that whenever the
        older of the two ends first.
        """
        self.base_health_grants = [
            grant for grant in self.base_health_grants if grant[0] is not by_effect]
        if self.base_health_grants:
            value = self.base_health_grants[-1][1]
        else:
            value = self.printed_health
        self.SetBaseHealth(value, by_effect)

    @final
    def SetBaseHealth(self, value: int, by_effect: 'Effect') -> None:
        # Moving base health by `diff` moves max health by `diff` and current
        # health with it -- one edit each, the same shape as `SetBaseAttack` and
        # `SetBaseScheme`, which move their one keyword once.
        #
        # `GainOnlyMaxHealth` *is* `UpdateMaxHealth`, so calling both applied the
        # max-health half twice while the current-health half ran once. A card
        # whose whole stat line is granted felt it on arrival: a Drone Minion has
        # no printed statistics, so Ultron Drones' `base_health=1` took a fresh
        # 0/0 drone to `health=1, max_health=2` and it entered play already
        # reading 1 damage. Lethality was unchanged -- 0 hit points is 0 hit
        # points -- and the v2 digest carries `health` and not `max_health`, so
        # nothing the corpus oracle reads could see it either. What could see it
        # is `sustained`, which is what a `CardFinder(canbe_heal=True)` asks
        # about: a fresh drone was a legal target for First Aid (01086) and three
        # other shipped cards, and healing it left a 2/2 drone that took two hits
        # to kill instead of one. See MARVEL-100.
        if self.base_health != value:
            diff = value - self.base_health
            self.base_health = value
            self.GainOnlyMaxHealth(diff, by_effect)
            self.GainOnlyHealth(diff, by_effect)

    ################################################################################
    #
    @final
    def UpdateMaxHealth(self, value: int, by_effect: 'Effect'):
        # The same in-play guard `UpdateHealth` applies, but it must require this
        # exact face. `components.health` belongs to the *card*, not to the face,
        # and `CardFace.IsInPlay()` deliberately treats another face with the
        # same name as equivalent. Villain stages share both a name and a card:
        # removing The "Immortal" Klaw can defeat stage I, advance to stage II,
        # and reveal the side scheme again between the health and max-health
        # halves of this one mutation. The retired stage-I face must not then
        # subtract from stage II's freshly reset maximum. See MARVEL-123.
        #
        # A card that leaves play may also have had the component zeroed --
        # `Card.Reset` runs `Health.OnParentReset`, and a facedown Ultron drone
        # reverting to the player card it was made from does exactly that.
        #
        # Without this, the two halves of `GainHealthAndMaxHealth` can disagree
        # about which object they are editing. Removing a +1 HP grant from a
        # drone sitting at 1 HP runs `GainOnlyHealth(-1)` first, which drops it
        # to 0, kills it, and reverts the card to its printed face with the
        # component reset to 0; `GainOnlyMaxHealth(-1)` then subtracts from the
        # *fresh* component and leaves `max_health = -1`. See MARVEL-77.
        if not self.IsInPlay(is_same_face=True):
            return
        self.components.health.AddMaxHealth(value)

    @final
    def UpdateHealth(self, value: int, by_effect: 'Effect', *, max_sustained: int|None=None) -> 'Message.AfterUnitHealHealth|Message.AfterUnitLossHealth|None':
        from game.card.face.base import Unit2
        if not self.IsInPlay():
            return None

        assert Unit2.IsType(self)

        def limit_health(health: int) -> int:
            if max_sustained != None:
                if self.max_health - health > max_sustained:
                    health = self.max_health - max_sustained
            return health

        if value == 0:
            return None
        elif value > 0:
            health = min(self.max_health, self.health + int(value))
            health = limit_health(health)
            value = health - self.health
            if value > 0:
                message = Message.WhenUnitHealHealth(self, value, by_effect)
                message.Send()
                self.components.health.AddHealth(value)
                after_message = Message.AfterUnitHealHealth(self, value, by_effect, message)
                after_message.Send()
                return after_message
            else:
                return None
            # return value
        else:
            message = Message.WhenUnitLossHealth(self, -1 * value, by_effect)
            message.Send()
            health = self.health + int(value)
            health = limit_health(health)
            # We use `Set` to make health can be a negative number
            self.components.health.SetHealth(health)
            after_message = Message.AfterUnitLossHealth(self, -1 * value, message)
            after_message.Send()
            return after_message
            # return -1 * value

    @final
    def LimitHealth(self, by_effect: 'Effect', check_death: bool=True):
        from game.card.face.base import Unit2
        assert Unit2.IsType(self)

        if self.is_infinite_health:
            pass
        elif self.health > self.max_health:
            self.SetHealth(self.max_health, by_effect)
        elif self.health <= 0 and self.IsInPlay() and check_death and not self.card.state.is_leaving_play:
            self.Death(None, by_effect)

    ################################################################################
    #
    @final
    def GainConsideredAtLeastHP(self, diff: int, by_effect: 'Effect') -> None:
        from game.card.face.base import Unit2
        if by_effect not in self.considered_at_least_hp_dict:
            self.considered_at_least_hp_dict[by_effect] = 0
        self.considered_at_least_hp_dict[by_effect] += diff
        if self.health < 0:
            self.Defeated(None, by_effect)
        message = Message.AfterUnitHitPointSet(self.CastTo(Unit2), by_effect)
        message.Send()
