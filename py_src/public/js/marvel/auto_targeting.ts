import { Lib } from './lib.js'
import { ButtonSetting, Setting } from './settings.js'
import { UI } from './ui.js'
import { Cards } from './cards.js'
import { Effect } from './effect.js'

///////////////////////////////////////////////////////////////////////////////
// Auto effect
export class AutoEffect {

    static isAutoEffectTargeting() {
        if( ButtonSetting.is_replay ) {
            return false;
        }
        if (!ButtonSetting.auto_target) {
            return false;
        }
        if (Effect.response_json_ask.options.length !== 1) {
            return false;
        }
        let name = Effect.response_json_ask.options[0].name_with_space;
        if (["Flip to alter-ego form", "Ask", "Change form", "Change Form", 'Defense'].includes(name)) {
            return false;
        }
        if (!Lib.object.isEmpty(Effect.response_json_ask.options[0].target_payment)) {
            return false;
        }
        if (Effect.response_json_ask.options[0].target_num_range[0] == 0 &&
            Effect.response_json_ask.options[0].target_num_range[1] == 0 ) {
            return false;
        }

        const object_id = Effect.response_json_ask.options[0].bind_id
        const card = Cards.getCard(object_id);
        if (card) {
            if (Setting.auto_effect_black_list.includes(card.card_id)) {
                return false;
            }
            if (!UI.is_in_play_turn) {
                // if (["Hero", "AlterEgo", "Support", "Upgrade", "Ally", "Resource"].includes(card.card_type)) {
                //     return true;
                // }
                return true
            }
        }
        return false;
    }
}

