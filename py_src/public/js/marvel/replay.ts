import { ButtonSetting } from './settings.js'
import { Effect } from './effect.js'
import { Button } from './buttons.js'
import { SelectStep } from './select.js'
import { BtnOk } from './btn_ok.js'
import { Cards } from './cards.js'

export class Replay {
    static prepared_replay = false
    static preparing_replay = false

    static doReplay(temp=false, delay=300, do_skip=false) {
        if( Replay.prepared_replay || Replay.preparing_replay ) {
            return false
        }
        Replay.preparing_replay = true
        if( Effect.response_json_ask.replay_input != '{}' ) {
        setTimeout(async () => {
            let data = JSON.parse(Effect.response_json_ask.replay_input)
            let is_debug_command = false
            function sleep(ms: number) {
                return new Promise(resolve => setTimeout(resolve, ms));
            }

            if( data['id'].startsWith(":") ) {
                is_debug_command = true
            }
            else if( data['id'] != '' ) {
                const object_id = Number(data['id'].match(/c(\d+) /)[1])
                const card_div = Cards.getDiv(object_id)!

                if( SelectStep.isCard() ) {
                    await sleep(delay);
                    Effect.onCardClick(card_div, false, true)
                }

                if( SelectStep.isEffect() ) {
                    await sleep(delay);
                    const str = data['id'].match(/e(\d+) (.*) c(\d+)/)[2].replaceAll(" ", "_")
                    let buttons = Effect.options_button_div.querySelectorAll('button')
                    if( str == "Cancel" ) {
                        Effect.onCancel()
                    }
                    else
                    if( buttons.length == 1 ) {
                        buttons[0].click()
                    }
                    else {
                        for( let e of buttons ) {
                            const button_text = JSON.parse(e.dataset.json_str!)['name']
                            if( str == button_text) {
                                e.click()
                                break
                            }
                        }
                    }
                }

                if( !SelectStep.isTargets() && !SelectStep.isCost() ) {
                    // Some debug command like `Play('04016')` will create new card that not in the list so
                    // `Effect.clicking_card_div` will get null and `Effect.isExEffect()` failed in `onCardClick`
                    // `SelectStep.step` still in `card`
                    if( !SelectStep.isCard() ) {
                        Button.doPost(false)
                    }
                }
                if( SelectStep.isTargets() && data['targets'].length > 0 ) {
                    await sleep(delay);
                    for( let res of data['targets'] ) {
                        const object_id = Number(res.match(/c(\d+) /)[1])
                        const card_div = Cards.getDiv(object_id)!
                        Effect.onCardClick(card_div, false, true)
                    }
                }

                if( !SelectStep.isCost() ) {
                    if( BtnOk.btn_end_div.classList.contains('ok') ||
                        !BtnOk.btn_ok_div.disabled ) {
                        Button.doPost(false)
                    } else {
                        // if( data['id'].includes('Cancel') ) {
                        //     Button.doCancel()
                        // }
                    }
                }
                if( SelectStep.isCost() &&  data['resources'].length > 0 ) {
                    await sleep(delay);
                    for( let res of data['resources'] ) {
                        const object_id = Number(res.match(/c(\d+) /)[1])
                        const card_div = Cards.getDiv(object_id)!
                        Effect.onCardClick(card_div, false, true)
                    }
                }
            }
            Replay.prepared_replay = true
            Replay.preparing_replay = false

            if( ButtonSetting.is_replay || temp ) {
                setTimeout(() => {
                    document.querySelectorAll('.deck.clicked').forEach( deck_div => {
                        deck_div.classList.remove('clicked')
                    })
                    if( is_debug_command ) {
                        Button.doNext()
                    }
                    else if( SelectStep.isCost() ) {
                        Button.doPost(true)
                    } else {
                        Button.doCancel()
                    }
                    if( do_skip ) {
                        Button.doToEnd()
                    }
                }, delay);
            }
        }, 100);
        }
    }

}
