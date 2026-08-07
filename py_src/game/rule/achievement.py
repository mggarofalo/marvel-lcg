from core import *
from game.ability import *
from game.message import *
from game.ability.factory import AbilityFactory
from game.player import *

def GetPlayAchievement() -> List['Ability']:
    def achievement_first_blood(effect: 'Effect', message: 'Message.AfterUnitBeDefeated'):
        from engine import Engine
        from game.operate.effects import Effects

        Effects.UnRegister(effect)
        assert message.killer != None
        player = message.killer.GetControlByPlayer()
        Engine.statistics.RecordAchievement(player, 'achievement_first_blood')

    def achievement_kill_count(effect: 'Effect', message: 'Message.AfterUnitBeDefeated'):
        from engine import Engine
        assert message.killer != None
        player = message.killer.GetControlByPlayer()
        data: Dict[int, Literal[
                    'achievement_double_kill',
                    'achievement_triple_kill',
                    'achievement_ultra_kill',
                    'achievement_rampage']
                ] = {
            2: 'achievement_double_kill',
            3: 'achievement_triple_kill',
            4: 'achievement_ultra_kill',
            5: 'achievement_rampage',
        }
        count = len(message.killer.GetControlByPlayer().stat.kill_counter)
        if count > 5:
            count = 5
        text = data[count]
        Engine.statistics.RecordAchievement(player, text)
        Engine.statistics.RecordMaximum('max_phase_kill', count)

    def achievement_none(effect: 'Effect', message: 'Message.WhenPlayerTurnEnd'):
        from game.operate.effects import Effects

        Effects.UnRegister(effect)
        player = message.GetToPlayer()
        if player.IsHero() and player.IsName("Steve Rogers") and player.hand_cards.GetSize() == 6 and player.player_deck.GetSize() == 34 and player.discard_pile.GetSize() == 0 and player.GetHero().IsReady():
            from engine.log import Notify
            name = player.GetHero().name
            Notify.Game(f"Ir{name[-5]}fr{name[5]}xs ({name[-2]})")

    return [
        AbilityFactory.AfterUnitBeDefeated(
            AbilityType.Statistics,
            None,
            achievement_first_blood,
            conditions=[
                lambda effect, message:
                    message.killer != None and \
                    Player.IsType(message.killer.GetControlBy()) and \
                    len(message.killer.GetControlByPlayer().stat.kill_counter) == 1 and \
                    not message.IsByConsequential()
            ],
        ),
        AbilityFactory.AfterUnitBeDefeated(
            AbilityType.Statistics,
            None,
            achievement_kill_count,
            conditions=[
                lambda effect, message:
                    message.killer != None and \
                    Player.IsType(message.killer.GetControlBy()) and \
                    len(message.killer.GetControlByPlayer().stat.kill_counter) >= 2 and \
                    not message.IsByConsequential()
            ],
        ),
        AbilityFactory.WhenPlayerTurnEnd(
            AbilityType.Statistics,
            "AnyPlayer",
            achievement_none
        ),
    ]

