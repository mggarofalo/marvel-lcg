from . import *

class HasTeamUp(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        self.team_up: List[List[str]] = [[], []]

        super().__init__(paper)

        # The team-up keyword names two characters.
        # In order for a player to include a card with the team-up keyword in their deck,
        # that player’s chosen identity must match one of the named characters.
        # Additionally, a card with the team-up keyword cannot be played unless
        # both of the named characters (identity or ally) are in play.
        def parse(value: str):
            team_up = value.split(";")
            self.team_up[0] = team_up[0].split("/") # "51025"
            self.team_up[1] = team_up[1].split("/")
        self.RegisterAttribute("TeamUp", parse)

    def GetTeamUpUnits(self) -> List['Ally|Identity']:
        from game.operate.worlds import Worlds

        faces: Set['Ally|Identity'] = set()
        units = Worlds.GetOnFieldFriendlyCharacters(self.card.game_area)
        for names in self.team_up:
            found = False
            for name in names:
                for unit in units:
                    if unit.IsName(name) or unit.IsSubName(name):
                        faces.add(unit)
                        found = True
                        break
            if not found:
                return []
        # `CardFace` defines no `__hash__`, so `faces` is hashed by memory
        # address and iterates in allocation order. This list becomes the
        # target list of a select effect, which is recorded verbatim in the
        # replay command, so that order has to be a property of the game state
        # and not of the allocator. Sort by `card.object_id` -- the same
        # pattern the optional-effect path applies to `Effect.object_id` in
        # `game/event/manager.py`, over a different id namespace. See
        # MARVEL-30.
        return sorted(faces, key=lambda face: face.card.object_id)

