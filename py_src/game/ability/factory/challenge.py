from . import *

class AbilityFactoryChallenge:

    @staticmethod
    def UsingOnlyHeroes(names: List[str]=[],
                        has_traits: List["CardFace.TRAITS"]|None=None,
                        title_has: str="",
                        has_or_can_gain_the_trait: "CardFace.TRAITS|None"=None,
                        alter_ego_has_traits: List["CardFace.TRAITS"]|None=None,
                        solo: bool=False) -> 'Ability':
        from game.ability.factory import AbilityFactory
        def check_heros(effect: 'Effect', message: 'Message.WhenGameBeginSetup') -> None:
            return
            # We didn't check this, players can follow the rule by themselves
            def check() -> bool:
                if solo:
                    if len(Worlds.GetPlayersIdentities(effect)) > 1:
                        return False
                if names:
                    for player in Worlds.ForEachPlayer(effect):
                        found = False
                        for name in names:
                            if player.IsName(name):
                                found = True
                                break
                        if not found:
                            return False
                if has_traits:
                    for player in Worlds.ForEachPlayer(effect):
                        found = False
                        for trait in has_traits:
                            identity = player.GetIdentity()
                            for face in [identity] + identity.card.back_faces:
                                if trait in face.paper.traits:
                                    found = True
                                    break
                        if not found:
                            return False
                if alter_ego_has_traits:
                    for player in Worlds.ForEachPlayer(effect):
                        found = False
                        for trait in has_traits:
                            identity = player.GetIdentity()
                            for face in [identity] + identity.card.back_faces:
                                if AlterEgo.IsType(face):
                                    if trait in face.paper.traits:
                                        found = True
                                        break
                        if not found:
                            return False
                return True
            if not check():
                effect.world.SetGameOver(False, effect)

        return AbilityFactory.WhenGameBeginSetup(
            check_heros
        )

    @staticmethod
    def WhenScenarioEnd(condition: Callable[['Effect', 'Message.WhenGameOver'], Literal[False]|None]|Literal[False]) -> 'Ability':
        def set_lost(effect: 'Effect', message: 'Message.WhenGameOver'):
            message.SetPlayerLost(effect)

        def check_condition(effect: 'Effect', message: 'Message.WhenGameOver'):
            if condition == False:
                return False
            res = condition(effect, message)
            if res == False:
                return True
            else:
                return False

        return Ability(
            AbilityType.Challenge,
            Message.WhenGameOver,
            [
                lambda effect, message:
                    message.players_won == True,
                check_condition,
            ],
            set_lost
        )

    @staticmethod
    def TheFinalBlowCanOnlyBeDealtBy(which_card: CardType) -> 'Ability':
        from game.ability.factory import AbilityFactory
        from game.card.face.base import Villain
        from game.card.face.card_type import Hero
        def is_final_blow(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> bool:
            if message.trigger.CastTo(Villain).IsFinalStage(effect):
                def is_1_health_hero():
                    if not message.attacker:
                        return False
                    if message.attacker.health != 1:
                        return False
                    if not Hero.IsType(message.attacker):
                        return False
                    return True
                return not is_1_health_hero()
            return False

        def health(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated'):
            message.trigger.SetHealth(1, effect)
            message.SetBeInstead(effect)

        return AbilityFactory.WhenUnitWouldBeDefeated(
            AbilityType.Challenge,
            Villain,
            health,
            conditions=[
                is_final_blow
            ]
        )

