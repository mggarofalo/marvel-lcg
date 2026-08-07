from core import *
from game.card.face import *
from game.ability import *
from game.player import *

class PlayerGet:
    def GetPlayer(self) -> 'Player':
        from game.player import Player
        return Cast(Player, self)

    ################################################################################
    #
    def GetSelfFieldAndHandCards(self) -> List['CardFace']:
        player = self.GetPlayer()
        return player.GetOnFieldCards() + player.hand_cards.Get()
    def GetOnFieldCards(self) -> List['CardFace']:
        player = self.GetPlayer()
        cards: List['CardFace'] = []
        faces = player.area_hero.GetAll() + \
            player.allies.GetAll() + \
            player.supports.GetAll()
        for face in faces:
            cards += [face] + face.GetInventoryDeck().GetAll()
        return cards
    def GetPlayerAreaCards(self) -> List['CardFace']:
        return self.GetOnFieldCards()
    # def GetUpgradeOrSupport(self) -> Sequence['Asset2|Support']:
    #     #  A player controls any upgrades attached to characters they control, including upgrades owned by another player.
    #     return list(self.supports.Get()) + list(self.GetControlUpgrade())
    #     # FAQ:
    #     # Am I the controller of an upgrade attached to an encounter card? For example, I am Valkyrie and I reveal Caught off Guard. The only upgrade in play is Death-Glow, attached to an enemy. Should I discard Death-Glow?
    #     # Yes, you are considered the controller of Death-Glow. Caught Off Guard can force you to discard Death-Glow from play.
    #     # Rule:
    #     # A player controls any upgrades attached to characters they control, including upgrades owned by another player
    #     # Stupid rule, how can you control a card that's is not in your inventory?
    #     # A player controls any upgrades attached to characters they control, including upgrades owned by another player.
    #     # from cards.pack import World
    #     # cards = World().GetOnFieldCards(self.GetIdentity().card.game_area)
    #     # return self.supports.Get() + [x for x in cards if Upgrade.IsType(x) and x.GetOwner() == self]
    def GetControlUpgrade(self, finder: 'CardFinder|None'=None) -> Sequence['Asset2']:
        """Include allies' upgrades"""
        from game.card.face.card_type import Upgrade
        from game.operate.worlds import Worlds
        player = self.GetPlayer()
        game_area = player.GetGameArea()
        upgrades = [
            y
            for x in self.GetControlCharacters()
            for y in x.GetInventoryDeck().Get()
            if Upgrade.IsType(y)
        ]
        check_faces = Worlds.GetOnFieldEnvironment(game_area) + Worlds.GetOnFieldEnemies(game_area) + Worlds.GetOnFieldSchemes(game_area)

        upgrades += [
            y
            for x in check_faces
            for y in x.GetInventoryDeck().Get()
            if Upgrade.IsType(y) and y.GetOwner() == player
        ]

        # Add `Attachment` for fix "16103"
        if finder:
            faces = finder.Checks(upgrades)
        else:
            faces = upgrades
        return faces
    def GetControlAttachment(self, finder: 'CardFinder|None'=None) -> Sequence['Asset2']:
        """Include allies' upgrades"""
        from game.card.face.card_type import Attachment
        upgrades = [x.GetInventoryDeck().Get() for x in self.GetControlCharacters()]
        # Add `Attachment` for fix "16103"
        faces = [y for x in upgrades for y in x if Attachment.IsType(y)]
        if finder:
            faces = finder.Checks(faces)
        return faces
    def GetControlPlayerSideScheme(self, finder: 'CardFinder|None'=None) -> Sequence['PlayerSideScheme']:
        from game.card.face.card_type import PlayerSideScheme
        from game.operate.worlds import Worlds
        faces: List['PlayerSideScheme'] = []
        player = self.GetPlayer()
        schemes = Worlds.GetSideSchemes(player.GetIdentity().card.game_area)
        for scheme in schemes:
            if PlayerSideScheme.IsType(scheme) and scheme.GetControlBy() == player:
                faces.append(scheme)
        return faces
    # def GetControlUpgradeOrSupport(self) -> Sequence['Asset2|Support']:
    #     player = self.GetPlayer()
    #     return list(self.GetControlUpgrade()) + list(player.supports.Get())
    def GetControlCharacters(self, finder: 'CardFinder|None'=None) -> List['Ally|AlterEgo|Hero']:
        player = self.GetPlayer()
        units = [self.GetIdentity()] + player.allies.Get()
        if finder:
            units = finder.Checks(units)
        # return [x for x in units if not x.IsDefeated()]
        return units

    def GetControlAllies(self, finder: 'CardFinder|None'=None) -> List['Ally']:
        player = self.GetPlayer()
        units = player.allies.Get()
        if finder:
            units = finder.Checks(units)
        # return [x for x in units if not x.IsDefeated()]
        return units

    def GetControlCardsByType(self,
                            finder: 'CardFinder|None'=None,
                            *,
                            ally: bool=False,
                            upgrade: bool=False,
                            support: bool=False,
                            identity: bool=False,
                            attachment: bool=False,
                            player_side_scheme: bool=False,
                            # control: bool=False,
                            ) -> List['CardFace']:
        faces: List['CardFace'] = []
        player = self.GetPlayer()

        # if control:
        #     ally = True
        #     upgrade = True
        #     support = True
        #     identity = True

        if ally:
            faces += player.allies.Get()
        if upgrade:
            faces += self.GetControlUpgrade()
        if support:
            faces += player.supports.Get()
        if identity:
            faces.append(player.GetIdentity())
        if attachment:
            faces += self.GetControlAttachment()
        if player_side_scheme:
            faces += self.GetControlPlayerSideScheme()

        if finder:
            faces = finder.Checks(faces)
        return faces

    TCA = TypeVar("TCA", bound='CardFace')
    def GetControlCards2(self, finder: 'CardFinder2[TCA]') -> List['TCA']:
        faces = self.GetControlCardsByType(finder, ally=True, upgrade=True, support=True, identity=True, attachment=False)
        return faces # type: ignore

    def GetControlCards(self, finder: 'CardFinder|None'=None) -> List['CardFace']:
        faces = self.GetControlCardsByType(finder, ally=True, upgrade=True, support=True, identity=True, attachment=False)
        return faces

    # def IsControlFace(self, CardFinder: 'CardFinder') -> bool:
    #     for face in self.GetControlCards():
    #         if CardFinder.Check(face):
    #             return True
    #     return False

    def IsControlAttachment(self, finder: 'CardFinder') -> bool:
        return self.GetControlCardsByType(finder, attachment=True) != []

    def IsControl(self, finder: 'CardFinder') -> bool:
        return self.GetControlCards(finder) != []

    # Call `Enemies.DoAttackYou`
    # def GetVillainAndEngagedMinions(self, effect: 'Effect') -> List['Villain|Minion']:
    #     from game.operate.worlds import Worlds

    #     player = self.GetPlayer()
    #     villain = Worlds.FindVillain(effect)
    #     units: List['Villain|Minion'] = [villain] if villain else []
    #     units += player.engaged_minions.Get(True)
    #     return units

    def GetEngagedMinions(self,
                        finder: 'CardFinder|None'=None,
                        ) -> List['Minion']:
        player = self.GetPlayer()
        faces = player.engaged_minions.Get(True)
        if finder:
            faces = finder.Checks(faces)
        return faces

    def GetIdentity(self) -> 'AlterEgo|Hero':
        from game.card.card_finder import CardFinder
        from game.card.face.card_type import AlterEgo
        from game.card.face.card_type import Hero

        player = self.GetPlayer()
        assert not player.IsScenario(), f"{self=}"
        if player.area_hero.GetSize() == 0:
            # identity = player.world.area_rule.GetTop()
            identity = player.world.insert
            assert identity != None
            return identity # type: ignore

        if player.area_hero.Get():
            return player.area_hero.Get()[0]
        else:
            faces = player.GetControlCards(CardFinder(card_type=AlterEgo|Hero))
            assert isinstance(faces[0], AlterEgo|Hero)
            return faces[0]
    def GetHero(self) -> 'Hero':
        from game.card.face.card_type import Hero
        hero = self.GetIdentity()
        if Hero.IsType(hero):
            return hero
        assert False, f"{hero=}"
    def GetAlterEgo(self) -> 'AlterEgo':
        from game.card.face.card_type import AlterEgo
        hero = self.GetIdentity()
        if AlterEgo.IsType(hero):
            return hero
        assert False, f"{hero=}"

    ################################################################################
    #
    def GetAllIdentityFace(self, exclude: 'CardFace|None'=None) -> List['AlterEgo|Hero']:
        from game.card.face.card_type import AlterEgo
        from game.card.face.card_type import Hero

        identities: List['AlterEgo|Hero'] = []

        identity = self.GetIdentity()
        identity_card_id = identity.paper.card_id[:-1]

        # Fix "31002a" (sp//dr)
        if identity.paper.card_id.startswith('3100'):
            for face in self.GetControlCardsByType(upgrade=True, support=True, identity=True):
                for check_face in [face] + face.card.back_faces:
                    if check_face == exclude:
                        continue
                    if isinstance(check_face, AlterEgo|Hero):
                        identities.append(check_face)
        # test_alter_ego
        elif identity_card_id.startswith('test_'):
            for check_face in [identity] + identity.card.back_faces:
                if check_face == exclude:
                    continue
                if isinstance(check_face, AlterEgo|Hero):
                    identities.append(check_face)
        else:
            for check_face in [identity] + identity.card.back_faces:
                if check_face == exclude:
                    continue
                if isinstance(check_face, AlterEgo|Hero):
                    # Fix for "29001a"
                    if check_face.paper.card_id[:-1] == identity_card_id:
                        identities.append(check_face)
        return identities

