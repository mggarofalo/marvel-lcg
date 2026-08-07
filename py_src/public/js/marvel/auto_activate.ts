import { Cards } from './cards.js'
import { ClassName } from './class_name.js'
import { Effect } from './effect.js';
import { HistoryLog } from './history.js';
import { Lib } from './lib.js';
import { Notify } from './notify.js'
import { ButtonSetting, Setting } from './settings.js';

export class AutoActivate
{
    static config: Set<string> = new Set()

    private static applyConfig() {
        document.querySelectorAll<HTMLElement>('.card').forEach(card_div =>
        {
            const object_id = Number(card_div.dataset.id!)
            const card_id = Cards.getCard(object_id)!.card_id
            if( AutoActivate.config.has(card_id) ) {
                card_div.classList.add(ClassName.auto_activate)
            }
            else if( card_div.classList.contains(ClassName.auto_activate) ) {
                // AutoActivate.config.add(card_id)
                card_div.classList.remove(ClassName.auto_activate)
            }
        })
    }

    static async loadConfig(hide_notification=false) {
        AutoActivate.config = Lib.cookie.getSet("auto_activate_faces")
    }

    static async loadConfigOld(hide_notification=false) {
        const response = await fetch(`get_auto_activate_config`);
        if (!response.ok) {
            throw new Error('Network response was not ok: ' + response.statusText);
        }

        const config = await response.json()
        if( Object.keys(config).length == 0 ) {
            if( !hide_notification ) {
                Notify.showResponse(`Load auto activate config failed`)
            }
            return
        }

        AutoActivate.config = new Set(config);
        HistoryLog.close()

        AutoActivate.applyConfig()

        if( !hide_notification ) {
            Notify.showResponse(`Loaded auto activate config`)
        }
    }

    static saveConfig() {
        Lib.cookie.setList("auto_activate_faces", Array.from(AutoActivate.config))
    }

    static saveConfigOld() {
        const data = JSON.stringify(Array.from(AutoActivate.config));
        const url = 'save_auto_activate_config';

        fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ data })
        })
        .then(async response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            const text = await response.text()
            HistoryLog.close()
            Notify.showResponse(text)
        })
        .then(responseData => {
            console.log('Success:', responseData);
        })
        .catch(error => {
            console.error('Error:', error);
        });
    }

    static isHasAutoActivate() {
        if( ButtonSetting.is_replay ) {
            return false
        }
        for( const option of Effect.response_json_ask.options ) {
            let name = option.name_with_space;
            if (["Flip to alter-ego form", "Ask", "Change form", "Change Form", 'Defense'].includes(name)) {
                return false;
            }
            const card_id = Cards.getCard(option.bind_id)?.card_id
            if( card_id && AutoActivate.config.has(card_id) ) {
                return true
            }
        }
        return false
    }

    static checkCanAutoActivate(object_id: number) {
        if( !ButtonSetting.auto_activate ) {
            return false
        }
        if( ButtonSetting.is_replay ) {
            return false
        }
        // if( Setting.is_hot_seat ) {
        //     return true
        // }
        const card = Cards.getCard(object_id)
        if( Setting.player_id == card?.control_player ) {
            return true
        } else {
            return false
        }
    }

    static isAutoActivate(object_id: number) {
        if( !AutoActivate.checkCanAutoActivate(object_id) ) {
            return false
        }
        const card = Cards.getCard(object_id)
        if( !card ) {
            return false
        }
        return AutoActivate.config.has(card.card_id)
    }

    static isAutoActivate2(card_div: HTMLElement) {
        const object_id = Number(card_div.dataset.id!)
        if( !AutoActivate.checkCanAutoActivate(object_id) ) {
            return false
        }
        return card_div.classList.contains(ClassName.auto_activate)
    }
}

(window as any).AutoActivate = AutoActivate;

