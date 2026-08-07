from core import *
from game.card.face import *
from game.ability import *
from game.message import *
from game.deck import *
from cards.pack import *
from game.ability.factory import AbilityFactory
from engine.config import ConfigVariables

ALLOW_CUSTOM_SCRIPT = ConfigVariables.Bool('allow_custom_script', False)

def GetGamePlayRules() -> List['Ability']:

    def try_shuffle_deck(effect: 'Effect', deck: 'Deck'):
        if effect.world.is_game_started:
            if deck.bind_discard_pile != None and deck.bind_discard_pile.GetSize() > 0:
                if not deck.flags.is_player_deck or not deck.GetOwnerPlayer().is_eliminated:
                    deck.ShuffleWithDiscardPile(True, effect)
            pass
            # # assert world.GetEncounterDeck().GetSize() != 0
            # # if world.GetEncounterDeck().GetSize() == 0:
            # for villain in World().GetScenario().area_villain.Get():
            #     if villain.encounter_deck.GetSize() == 0 and \
            #         villain.encounter_discard_pile.GetSize() > 0:
            #         Send.AfterDeckRunOut(villain.encounter_deck)
            #         villain.encounter_deck.ShuffleWithDiscardPile(True, effect)

            # for deck in World().additional_decks:
            #     if deck.GetSize() == 0 and \
            #         deck.bind_discard_pile != None and deck.bind_discard_pile.GetSize() > 0:
            #         Send.AfterDeckRunOut(deck)
            #         deck.ShuffleWithDiscardPile(True, effect)

            # for player in World().const_seat_order_players:
            #     if player.player_deck.GetSize() == 0 and player.discard_pile.GetSize() > 0 and not player.is_eliminated:
            #         Send.AfterDeckRunOut(player.player_deck)
            #         player.player_deck.ShuffleWithDiscardPile(True, effect)
            #     pass

    def increase_kill_count(effect: 'Effect', message: 'Message.AfterUnitBeDefeated'):
        assert message.killer != None
        player = message.killer.GetControlByPlayer()
        player.stat.UpdateKillCounter(message.trigger.CastTo(Unit2))

    def custom_script(effect: 'Effect', message: 'Message.WhenGameWouldBegin'):
        if not ALLOW_CUSTOM_SCRIPT.value:
            return

        # custom_script = effect.world.scene.campaign.custom_script

        # def remove_empty_lines(multi_line_string: str) -> str:
        #     """Remove all empty lines from a multi-line string."""
        #     # Split the string into lines and filter out empty lines
        #     non_empty_lines = [line for line in multi_line_string.splitlines() if line.strip()]
            
        #     # Join the non-empty lines back into a single string
        #     return '\n'.join(non_empty_lines)

        # custom_script = remove_empty_lines(custom_script)

        # def replace_start_tabs_with_spaces(multi_line_string: str) -> str:
        #     """Replace leading tabs in each line with four spaces."""
        #     lines = multi_line_string.splitlines()
        #     return '\n'.join(
        #         ' ' * (len(line) - len(line.lstrip('\t'))) * 4 + line.lstrip('\t')
        #         for line in lines
        #     )

        # custom_script = replace_start_tabs_with_spaces(custom_script)

        # def check_leading_indent(line: str) -> str:
        #     leading_spaces = len(line) - len(line.lstrip(' '))
        #     return ' ' * leading_spaces

        # leading_indent = check_leading_indent(custom_script)

        # def remove_start_string(multi_line_string: str, x: str):
        #     """Remove the specified string from the start of each line."""
        #     lines = multi_line_string.splitlines()
        #     return '\n'.join(
        #         line[len(x):] if line.startswith(x) else line
        #         for line in lines
        #     )

        # custom_script = remove_start_string(custom_script, leading_indent)

        # try:
        #     exec(custom_script)
        # except:
        #     pass

    return [
        Ability(
            AbilityType.Rule,
            Message.WhenGameWouldBegin,
            [],
            custom_script
        ),
        Ability(
            AbilityType.Rule,
            Message.AfterDeckRunOut,
            [],
            lambda effect, message:
                try_shuffle_deck(effect, message.deck),
        ),
        AbilityFactory.AfterUnitBeDefeated(
            AbilityType.Rule,
            None,
            increase_kill_count,
            conditions=[
                lambda effect, message:
                    message.killer != None and \
                    Player.IsType(message.killer.GetControlBy()) and \
                    not message.IsByConsequential()
            ]
        ),
    ]

