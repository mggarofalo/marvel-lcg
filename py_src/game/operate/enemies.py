from . import *

@final
class Enemies:

    class ActivatedResult:
        def __init__(self, has_enemy: bool=False, activated_cnt: int=0) -> None:
            self.has_enemy = has_enemy
            self.activated_cnt = activated_cnt

        def __add__(self, other: 'Enemies.ActivatedResult') -> 'Enemies.ActivatedResult':
            return Enemies.ActivatedResult(
                self.has_enemy | other.has_enemy,
                self.activated_cnt + other.activated_cnt
            )

        def __bool__(self) -> NoReturn:
            raise TypeError("This class cannot be converted to a boolean.")

    @staticmethod
    def DoActivateAgainstYouInternal(effect: 'Effect',
                                    activate_against_player: 'Player',
                                    *,
                                    activate: Literal["Attack", "Scheme", "Activate"],
                                    finder: 'CardFinder|None'=None,
                                    include_minion: Literal["All", "Engaged"],
                                    include_villain: bool,
                                    if_no_activate: Callable[[], Any]|None=None,
                                    ) -> 'ActivatedResult':
        from game.operate.worlds import Worlds
        # from game.ability.rule import Activate

        engaged_minions: bool = include_minion == "Engaged"
        all_minions: bool = include_minion == "All"

        active_cnt = 0
        has_enemy = False

        if include_villain:
            game_area = activate_against_player.GetIdentity().card.GetGameArea()
            if Worlds.GetScenario(effect).IsVillainReady():
                active_villain = Worlds.FindVillainByGameArea(game_area)
                if active_villain:
                    has_enemy = True
                    if not finder or finder.Check(active_villain):
                        if activate == "Attack":
                            do_active = active_villain.DoAttackYou(activate_against_player, effect)
                        elif activate == "Scheme":
                            do_active = active_villain.DoSchemes(activate_against_player, effect)
                        else:
                            do_active = active_villain.DoActivate(activate_against_player, effect)
                        if do_active:
                            active_cnt += 1
                if Worlds.IsGameOver(effect):
                    return Enemies.ActivatedResult(has_enemy, active_cnt)

        activated_minions: List['Minion'] = []
        while engaged_minions or all_minions:

            if engaged_minions:
                minions = activate_against_player.GetEngagedMinions(finder)
            elif all_minions:
                minions = Worlds.GetOnFieldMinions(effect, finder)
            else:
                assert False
            minions = [x for x in minions if x not in activated_minions]

            if not minions:
                break

            for minion in activate_against_player.ChooseOrder(minions, prompt="Minion Activates Order"):
                # Fix "35028"
                # if not minion.IsDefeated():
                if minion.IsInPlay():
                    has_enemy = True
                    if activate == "Attack":
                        do_active = minion.DoAttackYou(activate_against_player, effect)
                    elif activate == "Scheme":
                        do_active = minion.DoSchemes(activate_against_player, effect)
                    else:
                        do_active = minion.DoActivate(activate_against_player, effect)
                    if do_active:
                        active_cnt += 1

                    activated_minions.append(minion)

                if Worlds.IsGameOver(effect):
                    return Enemies.ActivatedResult(has_enemy, active_cnt)

                new_minions = activate_against_player.GetEngagedMinions(finder)
                # for x in new_minions:
                #     if x not in minions and x not in activated_minions:
                # for x in minions:
                #     if x not in new_minions:
                # New minion added
                if [x for x in new_minions if x not in minions and x not in activated_minions]:
                    break
                # An non-activated minion removed
                if [x for x in minions if x not in new_minions and x not in activated_minions]:
                    break

        if if_no_activate and active_cnt == 0:
            if_no_activate()

        return Enemies.ActivatedResult(has_enemy, active_cnt)

    ################################################################################
    # Minion
    @staticmethod
    def AllMinionActivateAgainstYou(effect: 'Effect',
                                    player: 'Player',
                                    *,
                                    trait: "CardFace.TRAITS|None"=None,
                                    finder: 'CardFinder|None'=None,
                                    if_no_activate: Callable[[], Any]|None=None,
                                    ):
        if trait and not finder:
            finder = CardFinder2(trait)
        return Enemies.DoActivateAgainstYouInternal(
            effect,
            player,
            activate="Activate",
            finder=finder,
            include_minion="All",
            include_villain=False,
            if_no_activate=if_no_activate,
        )

    @staticmethod
    def AllMinionActivateAgainstEngagedPlayer(effect: 'Effect',
                                            *,
                                            trait: "CardFace.TRAITS|None"=None,
                                            finder: 'CardFinder|None'=None,
                                            if_no_activate: Callable[[], Any]|None=None):
        from game.operate.players import Players

        if trait and not finder:
            finder = CardFinder2(trait)

        def action(player: 'Player'):
            return Enemies.DoActivateAgainstYouInternal(
                effect,
                player,
                activate="Activate",
                finder=finder,
                include_minion="Engaged",
                include_villain=False,
            )

        activated_result = Players.ForEachPlayer(effect, action, Enemies.ActivatedResult())
        if activated_result.activated_cnt == 0 and if_no_activate:
            if_no_activate()
        return activated_result

    @staticmethod
    def AllMinionAttackTheHeroItEngagedWith(effect: 'Effect',
                                            *,
                                            trait: "CardFace.TRAITS|None"=None,
                                            finder: 'CardFinder|None'=None,
                                            if_no_attack: Callable[[], Any]|None=None):
        from game.operate.players import Players
        if trait and not finder:
            finder = CardFinder2(trait)

        def action(player: 'Player'):
            if not player.IsHero():
                return Enemies.ActivatedResult()
            return Enemies.DoActivateAgainstYouInternal(
                effect,
                player,
                activate="Attack",
                finder=finder,
                include_minion="Engaged",
                include_villain=False,
            )

        activated_result = Players.ForEachPlayer(effect, action, Enemies.ActivatedResult())
        if activated_result.activated_cnt == 0 and if_no_attack:
            if_no_attack()
        return activated_result

    @staticmethod
    def EngagedMinionActivateAgainstYou(effect: 'Effect',
                                    player: 'Player',
                                    *,
                                    trait: "CardFace.TRAITS|None"=None,
                                    finder: 'CardFinder|None'=None):
        if trait and not finder:
            finder = CardFinder2(trait)
        return Enemies.DoActivateAgainstYouInternal(
            effect,
            player,
            activate="Activate",
            finder=finder,
            include_minion="Engaged",
            include_villain=False
        )

    @staticmethod
    def EngagedMinionAttackYou(effect: 'Effect',
                            player: 'Player',
                            *,
                            trait: "CardFace.TRAITS|None"=None,
                            finder: 'CardFinder|None'=None):
        if trait and not finder:
            finder = CardFinder2(trait)
        return Enemies.DoActivateAgainstYouInternal(
            effect,
            player,
            activate="Attack",
            finder=finder,
            include_minion="Engaged",
            include_villain=False
        )

    @staticmethod
    def EngagedMinionSchemes(effect: 'Effect',
                            player: 'Player'):
        return Enemies.DoActivateAgainstYouInternal(
            effect,
            player,
            activate="Scheme",
            include_minion="Engaged",
            include_villain=False
        )

    ################################################################################
    # Villain and Minion
    @staticmethod
    def VillainAndEngagedMinionsActivateAgainstYou(effect: 'Effect',
                                                    player: 'Player'):
        return Enemies.DoActivateAgainstYouInternal(
            effect,
            player,
            activate="Activate",
            include_minion="Engaged",
            include_villain=True
        )

    @staticmethod
    def VillainAndEngagedMinionsAttackYou(effect: 'Effect',
                                          player: 'Player'):
        return Enemies.DoActivateAgainstYouInternal(
            effect,
            player,
            activate="Attack",
            finder=None,
            include_minion="Engaged",
            include_villain=True
        )

    ################################################################################
    # All Enemy
    @staticmethod
    def AllEnemyAttackYou(effect: 'Effect',
                        player: 'Player',
                        finder: 'CardFinder|None'=None,
                        ):
        return Enemies.DoActivateAgainstYouInternal(
            effect,
            player,
            activate="Attack",
            finder=finder,
            include_minion="All",
            include_villain=True
        )

    @staticmethod
    def AllEnemySchemeAgainstYou(effect: 'Effect',
                                player: 'Player',
                                finder: 'CardFinder|None'=None,
                                if_no_activate: Callable[[], Any]|None=None,
                                ):
        return Enemies.DoActivateAgainstYouInternal(
            effect,
            player,
            activate="Scheme",
            finder=finder,
            include_minion="All",
            include_villain=True,
            if_no_activate=if_no_activate,
        )

    ################################################################################
    #
    @staticmethod
    def PutYouDeckTopCardAsFacedownMinion(player: 'Player', name: str, by_effect: 'Effect', size: int=1):
        from game.card.factory import CardFactory
        world = player.world
        def process(face: 'CardFace'):
            paper = CardFactory.FindCardPapers(name)[0]
            ultron_facedown_drone = CardFactory.CreateFace(paper, world)
            face.card.SetAsCard(ultron_facedown_drone)
            minion = face.card.face.CastTo(Minion)
            minion.PutIntoPlay(player, by_effect)
        player.PopPlayerDeckCards(size, process, by_effect)

