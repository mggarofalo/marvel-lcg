from core import *

from game.card.face import *

from game.ability import *
from game.message import *

from game.ability.factory import *
from game.ability.condition import Condition
Unused(Condition)

from game.ability.factory.campaign import AbilityFactoryCampaign
Unused(AbilityFactoryCampaign)

from game.message import OnEvent
Unused(OnEvent)

from game.player import Player, Scenario, PlayerFinder
Unused(Player, Scenario, PlayerFinder)
from game.player import Form
Unused(Form)

from game.world.game_area import GameArea
from game.element.resources import Resources
from game.element.cost import Cost
Unused(GameArea)
Unused(Resources)
Unused(Cost)

from game.selector import Select
Unused(Select)

from game.card import Card
Unused(Card)

from game.deck import Deck, Deck2, DeckType
Unused(Deck, Deck2, DeckType)


from game.card.factory import CardFactory
Unused(CardFactory)

from game.operate.rand import Rand
Unused(Rand)

from game.effect.rule import ResetKeyword
from game.effect.rule import MainSchemePhasePlaceThreat
Unused(ResetKeyword)
Unused(MainSchemePhasePlaceThreat)

from game.element.damage_property import DamageProperty
Unused(DamageProperty)

from game.operate.faces import Faces
from game.operate.faces_counter import FacesCounter
from game.operate.filter import Filter
from game.operate.effects import Effects
from game.operate.search import Search
from game.operate.find import Find
from game.operate.players import Players
from game.operate.campaign_logs import CampaignLog
from game.operate.utility import Utility
Unused(Faces)
Unused(FacesCounter)
Unused(Filter)
Unused(Effects)
Unused(Search)
Unused(Find)
Unused(Players)
Unused(CampaignLog)
Unused(Utility)

from game.operate.run_at import RunAt
from game.operate.setup_cards import SetupCards
from game.operate.worlds import Worlds
from game.operate.modular_set import AsideModularSet
from game.operate.store import Stores
Unused(RunAt)
Unused(SetupCards)
Unused(Worlds)
Unused(AsideModularSet)
Unused(Stores)

from game.element.utility import IsCounter, IsTrait
Unused(IsCounter, IsTrait)

from game.buff import *

CardFinder.NonEliteMinion = CardFinder(non_trait="ELITE", card_type=Minion)
# CardFinder2.Null = CardFinder2()
NonEliteMinion = CardFinder.NonEliteMinion

################################################################################
#
def ShuffleThisCardIntoEncounter(by_effect: 'Effect', message: 'Message2') -> None:
    this = by_effect.this
    encounter_deck = Worlds.GetEncounterDeck(by_effect)
    Faces.ShuffleAllTo([this], encounter_deck, by_effect)

def ThisCardGainSurge(by_effect: 'Effect', message: 'Message2|None'=None) -> None:
    this = by_effect.this.CastTo(CanSurge)
    this.GainSurge(1, by_effect)

def DiscardThisCard(by_effect: 'Effect', message: 'Message2') -> None:
    this = by_effect.this
    Faces.DiscardAll([this], by_effect)

def RemoveThisFromGame(by_effect: 'Effect', message: 'Message2') -> None:
    this = by_effect.this
    Faces.RemoveAllFromGame([this], by_effect)

def DiscardSelectedCards(by_effect: 'Effect', message: 'Message2') -> None:
    Faces.DiscardAll(by_effect.targets, by_effect)

def PutThisIntoPlay(by_effect: 'Effect', message: 'TriggerNonePlayerMessage') -> None:
    this = by_effect.this
    player = message.GetToPlayer()
    this.PutIntoPlay(player, by_effect)

def RevealThisCard(by_effect: 'Effect', message: 'TriggerNonePlayerMessage') -> None:
    this = by_effect.this
    player = message.GetToPlayer()
    this.Reveal(player, by_effect)

################################################################################
# Player
"""
Player.DiscardDeckTopCards
Player.DiscardDeckUntil
"""

def YouMayFlipToYourAlterEgoForm(player: 'Player', effect: 'Effect'):
    if player.IsHero():
        player.MayChooseOneAbility(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Flip to alter-ego form",
                lambda targets:
                    player.GetIdentity().ChangeToForm(AlterEgo, effect)
            )
        )

def YouMayChangeForm(player: 'Player', effect: 'Effect'):
    identity = player.GetIdentity()

    face = player.MayChooseOneText(player.GetAllIdentityFace(exclude=identity))
    if face:
        player.GetIdentity().ChangeToFace(face, effect)

