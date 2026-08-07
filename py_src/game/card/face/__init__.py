# from typing import TypeAlias
from core import *
from core import Unused

TC = TypeVar("TC", bound='CardFace', covariant=True)
TF = TypeVar("TF", bound='CardFace')
Unused(TC, TF)

# INT_TYPE: TypeAlias = int|Literal["1*", "2*", "3*", "4*", "5*", "6*", "9*", "10*", "12*", "18*"]
INT_TYPE = int|Literal["1*", "2*", "3*", "4*", "5*", "6*", "9*", "10*", "12*", "18*"]

from game.card.face.card_face import CardFace
from game.card.card_finder import CardFinder, CardFinder2
from game.card.card_finder import CardFinderHelper
Unused(CardFace)
Unused(CardFinder, CardFinder2)
Unused(CardFinderHelper)

################################################################################
#
from game.card.face.attribute.power.power_property import PowerProperty
Unused(PowerProperty)

from game.card.face.attribute.has_attribute import HasAttribute
Unused(HasAttribute)

from game.card.face.attribute.can_attack import AttackProperty
from game.card.face.attribute.can_defense import DefenseProperty
from game.card.face.attribute.can_recover import RecoverProperty
from game.card.face.attribute.can_scheme import SchemeProperty
from game.card.face.attribute.can_thwart import ThwartProperty
Unused(AttackProperty)
Unused(DefenseProperty)
Unused(RecoverProperty)
Unused(SchemeProperty)
Unused(ThwartProperty)

################################################################################
#
from game.card.face.attribute.can_place_counter import CanPlaceCounter
from game.card.face.attribute.can_place_token import CanPlaceToken
Unused(CanPlaceCounter)
Unused(CanPlaceToken)

from game.card.face.attribute.can_attacked import CanAttacked
from game.card.face.attribute.has_acceleration_icon import HasAccelerationIcon
from game.card.face.attribute.can_acceleration_token import CanAccelerationToken
from game.card.face.attribute.can_attach import CanAttach
from game.card.face.attribute.has_amplify import HasAmplify
from game.card.face.attribute.can_attack import HasAttack
from game.card.face.attribute.can_attack import CanAttack
from game.card.face.attribute.can_boost import CanBoost
from game.card.face.attribute.has_crisis import CanCrisis
from game.card.face.attribute.can_defense import HasDefense
from game.card.face.attribute.can_defense import CanDefense
from game.card.face.attribute.has_hazard import HasHazard
from game.card.face.attribute.can_health import CanHealth
from game.card.face.attribute.can_hinder import CanHinder
from game.card.face.attribute.can_incite import CanIncite
from game.card.face.attribute.can_recover import HasRecover
from game.card.face.attribute.can_recover import CanRecover
from game.card.face.attribute.can_retaliate import CanRetaliate
from game.card.face.attribute.can_scheme import HasScheme
from game.card.face.attribute.can_scheme import CanScheme
from game.card.face.attribute.can_surge import CanSurge
from game.card.face.attribute.can_thwart import HasThwart
from game.card.face.attribute.can_thwart import CanThwart
from game.card.face.attribute.has_victory import HasVictory
from game.card.face.attribute.has_setup import HasSetup
from game.card.face.attribute.has_uses import HasUses
Unused(CanAttacked)
Unused(HasAccelerationIcon)
Unused(CanAccelerationToken)
Unused(CanAttach)
Unused(HasAmplify)
Unused(HasAttack)
Unused(CanAttack)
Unused(CanBoost)
Unused(CanCrisis)
Unused(HasDefense)
Unused(CanDefense)
Unused(HasHazard)
Unused(CanHealth)
Unused(CanHinder)
Unused(CanIncite)
Unused(HasRecover)
Unused(CanRecover)
Unused(CanRetaliate)
Unused(HasScheme)
Unused(CanScheme)
Unused(CanSurge)
Unused(HasThwart)
Unused(CanThwart)
Unused(HasVictory)
Unused(HasSetup)
Unused(HasUses)

from game.card.face.attribute.has_hand_size import HasHandSize
from game.card.face.attribute.has_modify import HasModify
from game.card.face.attribute.has_stage import HasStage
from game.card.face.attribute.has_boost_icon import HasBoostIcon
from game.card.face.attribute.has_cost import HasCost
from game.card.face.attribute.has_assault import HasAssault
from game.card.face.attribute.has_form import HasForm
from game.card.face.attribute.has_restricted import HasRestricted
from game.card.face.attribute.has_resources import HasResourceIcon
from game.card.face.attribute.has_permanent import HasPermanent
from game.card.face.attribute.has_temporary import HasTemporary
from game.card.face.attribute.has_teamup import HasTeamUp
from game.card.face.attribute.can_teamwork import CanTeamwork
from game.card.face.attribute.has_alliance import HasAlliance
from game.card.face.attribute.can_quickstrike import CanQuickstrike
from game.card.face.attribute.has_guard import HasGuard
from game.card.face.attribute.has_patrol import HasPatrol
from game.card.face.attribute.has_villainous import HasVillainous
from game.card.face.attribute.has_max_per import HasMaxPer
from game.card.face.attribute.has_vulnerable import HasVulnerable
from game.card.face.attribute.has_toughness import HasToughness
from game.card.face.attribute.has_steady import HasSteady
from game.card.face.attribute.has_stalwart import HasStalwart
from game.card.face.attribute.can_status import CanStatus, CanNoStatus
from game.card.face.attribute.has_peril import HasPeril
Unused(HasHandSize)
Unused(HasModify)
Unused(HasStage)
Unused(HasBoostIcon)
Unused(HasCost)
Unused(HasAssault)
Unused(HasForm)
Unused(HasRestricted)
Unused(HasResourceIcon)
Unused(HasPermanent)
Unused(HasTemporary)
Unused(HasTeamUp)
Unused(CanTeamwork)
Unused(HasAlliance)
Unused(CanQuickstrike)
Unused(HasGuard)
Unused(HasPatrol)
Unused(HasVillainous)
Unused(HasMaxPer)
Unused(HasToughness)
Unused(HasSteady)
Unused(HasStalwart)
Unused(HasVulnerable)
Unused(CanStatus, CanNoStatus)
Unused(HasPeril)

################################################################################
#
from game.card.face.base.card_encounter import EncounterCard
from game.card.face.base.card_encounter import EncounterNonVillainCard
from game.card.face.base.card_player import PlayerCard
from game.card.face.base.card_player import ClassCard
Unused(EncounterCard)
Unused(EncounterNonVillainCard)
Unused(PlayerCard)
Unused(ClassCard)

from game.card.face.base.asset import Asset2
from game.card.face.base.scheme import Scheme2
from game.card.face.base.scheme_side import SchemeSide2
from game.card.face.base.unit import Unit2
from game.card.face.base.enemy import Enemy
from game.card.face.base.friend import Friend
from game.card.face.base.villain import Villain
Unused(Asset2)
Unused(Scheme2)
Unused(SchemeSide2)
Unused(Unit2)
Unused(Enemy)
Unused(Friend)
Unused(Villain)

from game.card.face.base.final_type import FinalType
Unused(FinalType)

################################################################################
#
# from game.card.face.component.component import Component

# from game.card.face.component.attach import PlacedCard
# from game.card.face.component.inventory import Inventory
# from game.card.face.component.counter import Counter
# from game.card.face.component.token import Token
# from game.card.face.component.health import Health
# from game.card.face.component.boostable import Boostable
# Unused(Component)
# Unused(PlacedCard)
# Unused(Inventory)
# Unused(Counter)
# Unused(Token)
# Unused(Health)
# Unused(Boostable)

################################################################################
# Card Type
from game.card.face.card_type.card_status import StatusCard
from game.card.face.card_type.insert import Insert, Challenge
from game.card.face.card_type.ally import Ally
from game.card.face.card_type.identity import Identity, Hero, AlterEgo
from game.card.face.card_type.minion import Minion
from game.card.face.card_type.villain import EncounterVillain
from game.card.face.card_type.leader import Leader

from game.card.face.card_type.scheme_side import EncounterSideScheme
from game.card.face.card_type.scheme_main import MainScheme
from game.card.face.card_type.scheme_player import PlayerSideScheme
from game.card.face.card_type.environment import Environment

from game.card.face.card_type.upgrade import Upgrade
from game.card.face.card_type.support import Support
from game.card.face.card_type.attachment import Attachment
from game.card.face.card_type.event import Event
from game.card.face.card_type.obligation import Obligation
from game.card.face.card_type.treachery import Treachery
from game.card.face.card_type.resource import Resource
from game.card.face.card_type.evidence import Evidence
Unused(StatusCard)
Unused(Insert)
Unused(Challenge)
Unused(Ally)
Unused(Identity, Hero, AlterEgo)
Unused(Minion)
Unused(EncounterVillain)
Unused(Leader)
Unused(EncounterSideScheme)
Unused(MainScheme)
Unused(PlayerSideScheme)
Unused(Environment)
Unused(Upgrade)
Unused(Support)
Unused(Attachment)
Unused(Event)
Unused(Obligation)
Unused(Treachery)
Unused(Resource)
Unused(Evidence)

