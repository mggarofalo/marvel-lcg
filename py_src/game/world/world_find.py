from core import *
from game.card.face import *
from game.deck import *
from game.effect import *
from game.player import *
from game.world import *
from game.world.game_area import *

class WorldFind:

    def GetWorld(self) -> 'World':
        from game.world import World
        return Cast(World, self)

    def CastGameArea(self, x: 'Effect|GameArea|None') -> 'GameArea':
        if isinstance(x, Effect):
            from game.message.message_type import TriggerPlayerMessage
            if isinstance(x, TriggerPlayerMessage):
                return x.GetToPlayer().GetIdentity().card.game_area
            else:
                return x.this.card.game_area
        if isinstance(x, GameArea):
            return x
        if x == None:
            world = self.GetWorld()
            return world.GetFirstGameArea()

    ################################################################################
    #
    def FindCardsOnField(self,
                        finder: 'CardFinder|None'=None,
                        *,
                        max: int=Math.INT_INF,
                        name: str|None=None,
                        trait: "CardFace.TRAITS|None"=None,
                        card_type: Type['TC']|CardFace=CardFace,
                        game_area: 'GameArea|None'=None,
                        **kwargs: Unpack['CardFinder.KWArgs']
                        ) -> List['TC']:
        world = self.GetWorld()
        areas = [
            *[world.GetScenario().area_villain],
            *[x.obligations_area for x in world.players],
            *[x.area_environment for x in world.players],
            world.area_schemes_main,
            world.area_schemes_side,
            world.area_environment
        ] + \
        [x.area_hero for x in world.const_players] + \
        [x.allies for x in world.const_players] + \
        [x.engaged_minions for x in world.const_players] + \
        [x.supports for x in world.const_players]

        return world.FindCardsInAreaInternal(
            areas=areas,
            finder=finder,
            max=max,
            name=name,
            trait=trait,
            card_type=card_type,
            game_area=game_area,
            **kwargs
        )

    def FindCardsInAreaInternal(self,
                                areas: List['Deck2[Any]'],
                                finder: 'CardFinder|None'=None,
                                *,
                                max: int=Math.INT_INF,
                                name: str|None=None,
                                trait: "CardFace.TRAITS|None"=None,
                                card_type: Type['TC']|CardFace=CardFace,
                                game_area: 'GameArea|None'=None,
                                **kwargs: Unpack['CardFinder.KWArgs']) -> List['TC']:
        from game.operate.filter import Filter
        cards: List[Any] = [] # We should also fix this late
        for area in areas:
            decks = [area] + [x.GetInventoryDeck() for x in area.Get() if isinstance(x, Unit2|Scheme2) and not x.IsDefeated()] + [x.GetPlacedCardArea() for x in area.Get() if isinstance(x, CardFace)]

            for deck in decks:
                cards += deck.GetAll(False)

        card_finder = CardFinder(
            name=name,
            trait=trait,
            card_type=card_type,
            game_area=game_area,
            **kwargs) & finder

        return Filter.ByFinder(
            cards,
            card_finder,
            max=max
        )

