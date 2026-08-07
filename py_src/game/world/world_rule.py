from core import *

class VariableBool:
    def __init__(self, rules: 'WorldRule', default: bool, name: str) -> None:
        self.val = default
        self.name_true = name
        self.name_false = f"no_{name}"

        self.version = name.split('_')[0] if name.startswith(('v15', 'v16')) else None
        rules.vars_bool.append(self)

    def __bool__(self) -> bool:
        return self.val

    def Set(self, val: bool):
        self.val = val

class WorldRule:
    def __init__(self) -> None:
        self.vars_bool: List['VariableBool'] = []

        self.ex_start_hand_size     : int = 0 # scene.ex_start_hand_size, always 0
        self.disable_shuffle        : bool = False

        self.show_all_cards                 = VariableBool(self, False, "show_all_cards")
        self.disable_pay                    = VariableBool(self, False, "disable_pay")
        self.disable_limit_once             = VariableBool(self, False, "disable_limit_once")
        self.disable_game_over              = VariableBool(self, False, "disable_game_over")
        self.disable_resolve_mulligans      = VariableBool(self, False, "disable_resolve_mulligans")
        self.additional_resolve_mulligans   = VariableBool(self, False, "additional_resolve_mulligans")
        self.disable_setup_draw_cards       = VariableBool(self, False, "disable_setup_draw_cards")
        self.create_player_deck_first       = VariableBool(self, False, "create_player_deck_first")

        self.v15_all                        = VariableBool(self, False, "v15_all")

        self.v16_all                        = VariableBool(self, False, "v16_all")
        self.v16_reveal                     = VariableBool(self, False, "v16_reveal")
        self.v16_teamwork                   = VariableBool(self, False, "v16_teamwork")
        self.v16_player_elimination         = VariableBool(self, False, "v16_player_elimination")
        self.v16_referential_ability        = VariableBool(self, False, "v16_referential_ability")
        self.v16_confuse_stun               = VariableBool(self, False, "v16_confuse_stun")

        self.fix_surge                      = VariableBool(self, True, "fix_surge")
        self.fix_treachery                  = VariableBool(self, True, "fix_treachery")
        self.encounter_cards_ignore_crisis  = VariableBool(self, True, "encounter_cards_ignore_crisis")
        self.crisis_of_infinite_deadpools   = VariableBool(self, True, "crisis_of_infinite_deadpools")
        self.mode_skirmish                  = VariableBool(self, False, "mode_skirmish")
        self.mode_campaign                  = VariableBool(self, False, "mode_campaign")
        self.mode_heroic                    : int = 0

    def SetRule(self, rules: List[str], is_puzzle: bool, seed: int):
        for var in self.vars_bool:
            if var.name_true in rules:
                var.Set(True)
            if var.name_false in rules:
                var.Set(False)

        self.ex_start_hand_size = 0 
        self.disable_shuffle    = seed == 0

        if is_puzzle:
            self.disable_setup_draw_cards = True
            self.disable_resolve_mulligans = True

        for rule in rules:
            if rule.startswith("mode_heroic_"):
                self.mode_heroic = int(rule.lstrip("mode_heroic_"))
                break

        if self.v16_all:
            for var in self.vars_bool:
                if var.version:
                    var.Set(var.version == "v16")

        if self.v15_all:
            for var in self.vars_bool:
                if var.version:
                    var.Set(var.version == "v15")

        # for var in self.vars_bool:
        #     if var.name_false in rules:
        #         var.Set(False)

