from core import *
from game.card.face import *
from game.ability import *
from game.buff import *
from game.effect import *
from game.player import *
from game.card.face.model.base import ModelBase
from game.element.resources import Resources

class ModelGain(ModelBase):

    @final
    def Gain(self, effect: 'Effect', diff: int,
                # forced: bool=False,
                trait: List["CardFace.TRAITS"]|"CardFace.TRAITS|None"=None,
                teamwork: List["CardFace.TRAITS"]|"CardFace.TRAITS|None"=None,
                attack: int|None=None,
                defense: int|None=None,
                thwart: int|None=None,
                scheme: int|None=None,
                retaliate: int|None=None,
                restricted: int|None=None,
                hand_size: int|None=None,
                stalwart: int|None=None,
                health: int|Literal["2*", "3*", "6*"]|Callable[['Effect'], int]|None=None,
                considered_at_least_hp: int|None=None,
                recover: int|None=None,
                guard: int|None=None,
                steady: int|None=None,
                treat_as_blank: bool|None=None,
                target_threat: int|None=None,
                quickstrike :int|None=None,
                patrol: int|None=None,
                amplify: int|None=None,
                crisis: int|None=None,
                ally_limit: int|None=None,
                flag: 'PlayerFlag.FLAG|None'=None,
                surge: int|None=None,
                hinder: int|Literal["2*"]|None=None,
                control_additional_restricted: 'CardFinder|None'=None,
                attack_consequential_damage: int|None=None,
                thwart_consequential_damage: int|None=None,
                consider_as: Tuple["CardFace.TRAITS", Type['TC']]|None=None,
                toughness: int|None=None,
                incite: int|None=None,
                acceleration_icon: int|None=None,
                hazard: int|None=None,
                peril: int|None=None,
                temporary: int|None=None,
                assault: int|None=None,
                have_res_icon: "Resources.RBYG|None"=None,
                permanent: int|None=None,
                villainous: int|None=None,
                ignore_acceleration_icon: int|None=None,
                ignore_amplify: int|None=None,
                ignore_crisis: int|None=None,
                ignore_hazard: int|None=None,
                additional_cost_for_basic_attack: 'CostFunc.Base|None'=None,
                additional_cost_for_basic_thwart: 'CostFunc.Base|None'=None,
                additional_cost_for_basic_defend: 'CostFunc.Base|None'=None,
                base_sch: int|None=None,
                base_atk: int|None=None,
                base_health: int|None=None,
                abilities: List[Ability]|None=None,
                buffs: List[Type['Buff']]|None=None,
                apply: Callable[['Effect','CardFace', int], None]|None=None,
                ) -> int:
        from game.card.face.attribute.has_hazard import HasHazard
        from game.card.face.attribute.has_amplify import HasAmplify
        from game.card.face.attribute.has_crisis import CanCrisis
        from game.card.face.attribute.can_attack import HasAttack
        from game.card.face.attribute.can_health import CanHealth
        from game.card.face.attribute.can_recover import HasRecover
        from game.card.face.attribute.can_retaliate import CanRetaliate
        from game.card.face.attribute.can_scheme import HasScheme
        from game.card.face.attribute.can_thwart import HasThwart
        from game.card.face.attribute.has_hand_size import HasHandSize
        from game.card.face.attribute.can_surge import CanSurge
        from game.card.face.attribute.can_hinder import CanHinder
        from game.card.face.attribute.has_acceleration_icon import HasAccelerationIcon
        from game.card.face.attribute.can_teamwork import CanTeamwork
        from game.card.face.attribute.can_incite import CanIncite
        from game.card.face.attribute.can_health import CanHealth
        from game.card.face.attribute.has_temporary import HasTemporary
        from game.card.face.attribute.has_permanent import HasPermanent
        from game.card.face.attribute.has_villainous import HasVillainous
        from game.card.face.attribute.has_restricted import HasRestricted
        from game.card.face.base import Unit2
        from game.card.face.base import EncounterNonVillainCard
        from game.card.face.base import Scheme2
        from game.card.face.card_type import Ally
        from game.card.face.card_type import Minion
        from game.card.face.card_type import Hero
        from game.card.face.card_type import MainScheme
        from game.operate.worlds import Worlds

        unit = self.GetThis()

        if unit.card.state.is_advancing:
            return 0

        return_val = 0

        if trait != None:
            unit.GainTraits(diff, trait, effect)
            return_val += 1
        if teamwork != None and CanTeamwork.IsType(unit):
            unit.GainTeamwork(diff, teamwork, effect)
            return_val += 1
        if thwart != None and HasThwart.IsType(unit):
            unit.GainThwart(thwart * diff, effect)
            return_val += thwart
        if scheme != None and HasScheme.IsType(unit):
            unit.GainScheme(scheme * diff, effect)
            return_val += scheme
        if attack != None and HasAttack.IsType(unit):
            unit.GainAttack(attack * diff, effect)
            return_val += attack
        if defense != None and Hero.IsType(unit): # CanDefense
            unit.GainDefense(defense * diff, effect)
            return_val += defense
        if retaliate != None and CanRetaliate.IsType(unit):
            unit.GainRetaliate(retaliate * diff, effect)
            return_val += retaliate
        if restricted != None and HasRestricted.IsType(unit):
            unit.GainRestricted(restricted * diff, effect)
            return_val += restricted
        if hand_size != None and HasHandSize.IsType(unit):
            unit.GainHandSize(hand_size * diff, effect)
            return_val += hand_size
        if stalwart != None and Unit2.IsType(unit):
            unit.GainStalwart(stalwart * diff, effect)
            return_val += stalwart
        if health != None and CanHealth.IsType(unit):
            # Hack, Fix "43036"
            # "Minion" "Ally"
            # if card_finder not in ["You", Identity, Friend]:
            #     need_gain_health = True
            # if need_gain_health:
            # else:
            assert Unit2.IsType(unit)

            if isinstance(health, int):
                value = health * diff
            elif isinstance(health, str):
                value = Worlds.ConvertPerPlayerIconToInt(health, effect)
                health = value
                value = value * diff
            else:
                health = health(effect)
                value = health * diff

            # Fix "13012"
            # if not unit.card.is_leaving_play and not unit.IsDefeated():
            if unit.card.state.is_leaving_play or \
                unit.health <= 0 and effect.this.card.state.is_leaving_play:
                pass
            elif unit.card.state.is_defeating and not unit.card.state.is_swapping_end and value <= 0:
                pass
            else:
                if unit.card.state.is_flipping:
                    unit.GainOnlyMaxHealth(value, effect)
                else:
                    unit.GainHealthAndMaxHealth(value, effect)
            return_val += health
        if considered_at_least_hp != None and CanHealth.IsType(unit):
            unit.GainConsideredAtLeastHP(considered_at_least_hp * diff, effect)
            return_val += considered_at_least_hp
        if recover != None and HasRecover.IsType(unit):
            unit.GainRecover(recover * diff, effect)
            return_val += recover
        if guard != None and Minion.IsType(unit):
            unit.GainGuard(guard * diff, effect)
            return_val += guard
        if steady != None and Unit2.IsType(unit):
            unit.GainSteady(steady * diff, effect)
            return_val += steady
        if treat_as_blank != None:
            unit.TreatAsIfBlankInternal(diff, effect)
            return_val += treat_as_blank
        if target_threat != None and MainScheme.IsType(unit):
            unit.GainTargetThreatValue(target_threat * diff, effect)
            return_val += target_threat
        if quickstrike != None and Minion.IsType(unit):
            unit.GainQuickstrike(quickstrike * diff, effect)
            return_val += quickstrike
        if patrol != None and Minion.IsType(unit):
            unit.GainPatrol(patrol * diff, effect)
            return_val += patrol
        if amplify != None and HasAmplify.IsType(unit):
            unit.GainAmplify(amplify * diff, effect)
            return_val += amplify
        if crisis !=  None and CanCrisis.IsType(unit):
            unit.GainCrisis(crisis * diff, effect)
            return_val += crisis

        if surge != None and CanSurge.IsType(unit):
            unit.GainSurge(surge * diff, effect)
            return_val += surge
        if hinder != None and CanHinder.IsType(unit):
            hinder = Worlds.ConvertPerPlayerIconToInt(hinder, effect)
            unit.GainHinder(hinder * diff, effect)
            return_val += hinder
        if toughness != None and Unit2.IsType(unit):
            unit.GainToughness(toughness * diff, effect)
            return_val += toughness
        if incite != None and CanIncite.IsType(unit):
            unit.GainIncite(incite * diff, effect)
            return_val += incite
        if acceleration_icon != None and HasAccelerationIcon.IsType(unit):
            unit.GainAccelerationIcon(acceleration_icon * diff, effect)
            return_val += acceleration_icon
        if hazard != None and HasHazard.IsType(unit):
            unit.GainHazard(hazard * diff, effect)
            return_val += hazard

        if ally_limit != None:
            player = unit.GetControlByPlayer()
            player.limit_ally.Increase(ally_limit * diff, effect)
            return_val += ally_limit
        if control_additional_restricted != None:
            player = unit.GetControlByPlayer()
            player.limit_restricted.Increase(diff, effect, control_additional_restricted)
            return_val += 1

        if flag != None:
            player = unit.GetControlByPlayer()
            player.flag.Update(flag, diff)

        if attack_consequential_damage != None and Ally.IsType(unit):
            unit.GainAttackConsequentialDamage(attack_consequential_damage * diff, effect)
            return_val += attack_consequential_damage
        if thwart_consequential_damage != None and Ally.IsType(unit):
            unit.GainThwartConsequentialDamage(thwart_consequential_damage * diff, effect)
            return_val += thwart_consequential_damage

        if peril != None and EncounterNonVillainCard.IsType(unit):
            unit.GainPeril(peril * diff, effect)
            return_val += peril

        if temporary != None and HasTemporary.IsType(unit):
            unit.GainTemporary(temporary * diff, effect)
            return_val += temporary

        if assault != None and Scheme2.IsType(unit):
            unit.GainAssault(assault * diff, effect)
            return_val += assault

        if have_res_icon != None and HasResourceIcon.IsType(unit):
            unit.printed_resource_internal += Resources(have_res_icon) * diff
            return_val += 1

        if permanent != None and HasPermanent.IsType(unit):
            unit.GainPermanent(permanent * diff, effect)

        if villainous != None and HasVillainous.IsType(unit):
            unit.GainVillainous(villainous * diff, effect)

        if ignore_acceleration_icon != None and HasAccelerationIcon.IsType(unit):
            unit.SetIgnoreAccelerationIcon(ignore_acceleration_icon * diff, effect)
            return_val += ignore_acceleration_icon
        if ignore_amplify != None and HasAmplify.IsType(unit):
            unit.SetIgnoreAmplify(ignore_amplify * diff, effect)
            return_val += ignore_amplify
        if ignore_crisis != None and CanCrisis.IsType(unit):
            unit.SetIgnoreCrisis(ignore_crisis * diff, effect)
            return_val += ignore_crisis
        if ignore_hazard != None and HasHazard.IsType(unit):
            unit.SetIgnoreHazard(ignore_hazard * diff, effect)
            return_val += ignore_hazard

        if abilities != None:
            if diff > 0:
                for ability in abilities:
                    unit.effect.RegisterGiven(ability)
            else:
                for ability in abilities:
                    unit.effect.UnRegisterGiven(ability)
            return_val += 1

        if buffs != None:
            if diff > 0:
                for buff in buffs:
                    unit.GetBuff(buff).OnGain(effect)
            else:
                for buff in buffs:
                    unit.GetBuff(buff).OnLost(effect)
            return_val += 1

        if apply != None:
            apply(effect, unit, diff)
            return_val += 1

        if consider_as != None:
            unit.SetConsiderAs(unit, diff, name=None, trait=consider_as[0], card_type=consider_as[1])
            return_val += 1

        if additional_cost_for_basic_attack:
            attack_effects = unit.effect.Find(func_name="ATK")
            for check_effect in attack_effects:
                if diff > 0:
                    check_effect.cost_func.Add(additional_cost_for_basic_attack)
                elif diff < 0:
                    check_effect.cost_func.Del(additional_cost_for_basic_attack)
            return_val += 1

        if additional_cost_for_basic_thwart:
            thwart_effects = unit.effect.Find(func_name="THW")
            for check_effect in thwart_effects:
                if diff > 0:
                    check_effect.cost_func.Add(additional_cost_for_basic_thwart)
                elif diff < 0:
                    check_effect.cost_func.Del(additional_cost_for_basic_thwart)
            return_val += 1

        if additional_cost_for_basic_defend:
            defend_effects = unit.effect.Find(func_name="DEF")
            for check_effect in defend_effects:
                if diff > 0:
                    check_effect.cost_func.Add(additional_cost_for_basic_defend)
                elif diff < 0:
                    check_effect.cost_func.Del(additional_cost_for_basic_defend)
            return_val += 1

        if base_sch and HasScheme.IsType(unit):
            unit.SetBaseScheme(base_sch * diff, effect)
            return_val += base_sch

        if base_atk and HasAttack.IsType(unit):
            unit.SetBaseAttack(base_atk * diff, effect)
            return_val += base_atk

        if base_health and CanHealth.IsType(unit):
            unit.SetBaseHealth(base_health * diff, effect)
            return_val += base_health

        return return_val * diff

    @final
    def FaceSet(self, effect: 'Effect', value: int,
                # forced: bool=False,
                attack: int|None=None,
                scheme: int|None=None,
                ) -> int:
        from game.card.face.attribute.can_scheme import HasScheme
        from game.card.face.attribute.can_attack import HasAttack

        unit = self.GetThis()
        return_val = 0

        if scheme != None and HasScheme.IsType(unit):
            unit.GainScheme(scheme * value - unit.scheme, effect)
            return_val += scheme
        if attack != None and HasAttack.IsType(unit):
            unit.GainAttack(attack * value - unit.attack, effect)
            return_val += attack

        return return_val * value

