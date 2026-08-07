import {Lib} from '../lib.js'
import {Setting} from '../settings.js'
import {Button} from '../buttons.js'
import {HoverCard} from '../hover.js'
import {HistoryLog} from '../history.js'
import {WindowLoad} from '../window.js'
import {Cards} from '../cards.js'
import { UI } from '../ui.js'
import { Client } from '../client.js'

////////////////////////////////////////////////////////////////////////////////
//
export class Debug {
    static debug_cmd_input_div: HTMLTextAreaElement
    private static debug_cmd_tooltip_box_div: HTMLTextAreaElement
    private static debug_next_frame_btn: HTMLButtonElement
    private static debug_cmd_tooltip_text_div: HTMLTextAreaElement
    static clicked_card_id: number = 0;
    private static line_height: number | undefined = Debug.debug_cmd_tooltip_text_div ? Debug.debug_cmd_tooltip_text_div.rows : undefined;
    static debug_message_div: HTMLElement;

    private static last_input: string[] = [];
    private static last_help_tip: string[] = [];
    private static help_tip = [
        '/T     <normal>        Test all cases in the normal category',
        '/T     <30>            Test all cases starting from the index 30',
        '/T     <30>:<10>       Test cases starting at the specified index and stopping at the step',
        '/C     <03001>         Comment this replay file',
        '/cs                    Comment this replay file using encounter_sets',
        '/R     <+1|-1>         Update replay index',
        '/CL    <all|uncall>    Show call log',
        'draw <5>',
        'gain <card> <3>',
        'gains <*card>',
        'play <card>',
        'ready <*card>',
        'exhaust <*card>',
        'discard <*card>',
        'remove <*card>',
        'flip <*card>',
        'heal <card> <30>',
        'damage <card> <30> [card]',
        'blank <*card>',
        'trait <card> "<trait>"',
        // 'counter <card> <name> [3]',
        'counter <card> "<name>" [3]',

        'Reveal <card>',
        'Boost <card>',
        'PutIntoPlay <card>',
        'putplay <card>',
        'putdeck <card> [1]',

        'confuse <card>',
        'stun <card>',
        'tough <card>',
        'cannot <card1> <card2> <...>',
        'can <card1> <card2> <...>',

        // 'PlaceThreat <card> [2]',
        'threat <card> [2]',
        'SetThreat <card> <num>',
        'ChangeForm <card> [Identity|Hero]',
    ];
    private static last_all_inputs: string[] = [];
    private static last_select_index = -1;
    static right_menu_card: HTMLElement
    static right_menu_deck: HTMLElement
    static right_menu_top: HTMLElement

    static async Fetch() {
        window.addEventListener('createCard', (event) => { 
            let customEvent = event as CustomEvent
            if (customEvent.detail && customEvent.detail.div) { 
                const card_div = customEvent.detail.div as HTMLElement; 
                card_div.oncontextmenu = (e) => { 
                    Debug.HideMenu()
                    Debug.onCardClick(card_div, true); 
                    e.stopPropagation();
                    e.preventDefault();
                }
                // card_div.addEventListener("click", ()=>{
                //     Debug.onCardClick(card_div, false)
                // })
            };
        })

        window.addEventListener('debugText', (event) => { 
            Debug.setDebugMessage(UI.debug_text)
        });

        // Fetch the content of debug.html
        await fetch('debug.html')
            .then(response => response.text())
            .then(data => {
                const debugContainer = document.createElement('div');
                // Set the innerHTML of the div to your debug code
                debugContainer.innerHTML = data;
                document.body.appendChild(debugContainer);
                Debug.Init()
                Debug.setDebugMessage(UI.debug_text)
        })
    }

    static setDebugMessage(text: string) {
        let div = document.getElementById("debug-window")
        if( div ) {
            div.innerHTML = `<div id="copy-debug-message"><i class="fa fa-clipboard" aria-hidden="true"></i></div>` + text
        }

        // Add event listener for copying text to clipboard
        document.getElementById('copy-debug-message')?.addEventListener('click', function() {
            // Create a temporary textarea element to hold the text
            const tempTextArea = document.createElement('textarea');
            tempTextArea.value = text;
            document.body.appendChild(tempTextArea);

            // Select the text and copy it to the clipboard
            tempTextArea.select();
            document.execCommand('copy');

            // Remove the temporary textarea element
            document.body.removeChild(tempTextArea);
        });


        const tables = document.querySelectorAll<HTMLTableElement>('.debug-table');
        tables.forEach((table: HTMLTableElement) => {
            const headers: NodeListOf<HTMLTableCellElement> = table.querySelectorAll('th');
            headers.forEach((header: HTMLTableCellElement, index: number) => {
                header.addEventListener('click', () => {
                    sortTable(table, index);
                });
            });
        });
        function sortTable(table: HTMLTableElement, columnIndex: number): void {
            const rows: HTMLTableRowElement[] = Array.from(table.querySelectorAll('tbody tr'));
            const isAscending = table.dataset.sortOrder === 'asc';

            rows.sort((a: HTMLTableRowElement, b: HTMLTableRowElement) => {
                const aText: string = a.children[columnIndex].textContent?.trim() || '';
                const bText: string = b.children[columnIndex].textContent?.trim() || '';

                const aValue: number | string = isNaN(Number(aText)) ? aText : parseFloat(aText);
                const bValue: number | string = isNaN(Number(bText)) ? bText : parseFloat(bText);
    
                return isAscending ? (aValue > bValue ? 1 : -1) : (aValue < bValue ? 1 : -1);
            });

            // Clear the table body and append sorted rows
            const tbody: HTMLTableSectionElement | null = table.querySelector('tbody');
            if (tbody) {
                tbody.innerHTML = '';
                rows.forEach((row: HTMLTableRowElement) => tbody.appendChild(row));
            }

            // Toggle sort order
            table.dataset.sortOrder = isAscending ? 'desc' : 'asc';
        }
    }

    static Init() {
        Debug.right_menu_card = document.querySelector<HTMLElement>('#debug-right-menu-card')!;
        Debug.right_menu_deck = document.querySelector<HTMLElement>('#debug-right-menu-deck')!;
        Debug.right_menu_top = document.querySelector<HTMLElement>('#debug-right-menu-options')!;
        Debug.debug_cmd_input_div = document.getElementById("debug-cmd-input") as HTMLTextAreaElement;
        Debug.debug_cmd_tooltip_box_div = document.getElementById("debug-cmd-tooltip-box") as HTMLTextAreaElement; // tooltip + click card + buttons
        Debug.debug_cmd_tooltip_text_div = document.getElementById("debug-cmd-text") as HTMLTextAreaElement; // tooltip
        Debug.debug_message_div = document.getElementById("debug-window") as HTMLElement;
        Debug.debug_next_frame_btn = document.getElementById("debug-buttons") as HTMLButtonElement;
        
        // debug right menu
        document.body.oncontextmenu = () => {
            if( Setting.is_debug ) {
                Debug.HideMenu()
                Debug.right_menu_top.classList.remove('hide');
                Debug.right_menu_top.style.left = `${WindowLoad.mouse_x + 5}px`;
                Debug.right_menu_top.style.top = `${WindowLoad.mouse_y + 5}px`;

                if( Debug.right_menu_top.offsetTop + Debug.right_menu_top.clientHeight > window.innerHeight ) {
                    let top = window.innerHeight - Debug.right_menu_top.clientHeight
                    Debug.right_menu_top.style.top = `${top}px`;
                }
            }
        }

        window.onclick = function(event) {
            let e = event.target as HTMLElement
            if( e.tagName.toUpperCase() != 'SUMMARY' ) {
                Debug.right_menu_card.classList.add('hide')
                Debug.setClickedCard(0)

                if( !e.classList.contains('deck') ){
                    Debug.right_menu_deck.classList.add('hide')
                    if( !e.classList.contains('not-hide-after-clicked') ) {
                        WindowLoad.onClick(event)
                    }
                }
            }
            Debug.right_menu_top.classList.add('hide')
        };

        window.onkeydown = function(event) {
            console.log(event.key)
            if (event.key === "`") {
                Debug.toggle()
                event.preventDefault();
            }
            else
            if( document.activeElement == Debug.debug_cmd_input_div ||
            !Debug.debug_cmd_input_div.classList.contains('hide')) {
                if( Debug.debug_message_div.classList.contains('hide-background') ) {
                    Debug.debug_cmd_input_div.focus()
                }
                Debug.onKeyDown(event)
            } else {
                WindowLoad.onKeyDown(event)
            }
        }

        Debug.last_all_inputs = Lib.cookie.getList("debug_inputs")

        Debug.debug_cmd_input_div.oninput = function() {
            let input = Debug.debug_cmd_input_div.value;
            Debug.last_help_tip = Debug.help_tip.filter(str => str.toLowerCase().startsWith(input.toLowerCase()));
            Debug.last_input = Debug.last_all_inputs.filter(str => str.toLowerCase().startsWith(input.toLowerCase()));
            Debug.last_select_index = -1;
            Debug.updateDebugTooltip();
        };
        Debug.last_input = Lib.array.deepClone(Debug.last_all_inputs);
        Debug.last_help_tip = Lib.array.deepClone(Debug.help_tip);
    }

    static HideMenu() {
        Debug.right_menu_top.classList.add('hide')
        Debug.right_menu_deck.classList.add('hide')
        Debug.right_menu_card.classList.add('hide')
    }

    static setClickedCard(object_id: number) { // 0 means null
        if( Debug.clicked_card_id ) {
            const card_div = Cards.getDiv(Debug.clicked_card_id)!
            card_div.classList.remove('debug-clicked')
        }
        Debug.clicked_card_id = object_id
        if( Debug.clicked_card_id ) {
            const card_div = Cards.getDiv(Debug.clicked_card_id)!
            card_div.classList.add('debug-clicked')
        }
    }

    static hideCmdTooltip() {
        Debug.debug_cmd_tooltip_text_div.rows = Debug.debug_cmd_tooltip_text_div.rows === 1 ? 10 : 1;
        Debug.line_height = Debug.debug_cmd_tooltip_text_div.rows;
        Debug.updateDebugTooltip();
    }

    static doDebugCmd(cmd: string) {
        if (cmd.includes("$0") && !Debug.clicked_card_id) {
            Lib.alert(cmd);
        } else {
            let text = cmd.replace('$0', `c${Debug.clicked_card_id}`);
            Debug.doEnter(text);
        }
    }

    static toggle() {
        if( !Setting.is_debug ) {
            return
        }
        Setting.cheat_show_all_cards = !Setting.cheat_show_all_cards
        Button.doShow()

        if( !Setting.cheat_show_all_cards ) {
            Client.onUpdateFinished(true)
        }

        Debug.line_height = Debug.debug_cmd_tooltip_text_div.rows;
        Debug.debug_cmd_input_div.classList.toggle('hide');
        if (Debug.debug_cmd_input_div.classList.contains('hide')) {
            Debug.debug_cmd_tooltip_text_div.classList.add('hide');
            Debug.debug_cmd_tooltip_box_div.classList.add('hide');
            Debug.debug_next_frame_btn.classList.add('hide');
            Debug.debug_message_div.classList.add('hide-background');
        } else {
            Debug.debug_cmd_tooltip_text_div.classList.remove('hide');
            Debug.debug_cmd_tooltip_box_div.classList.remove('hide');
            Debug.debug_message_div.classList.remove('hide-background');
            Debug.debug_next_frame_btn.classList.remove('hide');
        }

        if (!Debug.debug_cmd_input_div.classList.contains('hide')) {
            Debug.debug_cmd_input_div.focus();
            Debug.last_all_inputs = Lib.cookie.getList("debug_inputs")
            Debug.updateDebugTooltip();
        }
    }

    static updateDebugTooltip() {
        const helpTips = Debug.last_help_tip.join("\n");
        const startIndex = Debug.last_select_index + 1;
        const endIndex = Debug.line_height ? Debug.line_height + startIndex : startIndex;
        const recentInputs = Debug.last_input.slice(startIndex, endIndex).reverse().join("\n");
        const texts = `${helpTips}\n\n${recentInputs}`;

        Debug.debug_cmd_tooltip_text_div.value = texts
        Debug.debug_cmd_tooltip_text_div.scrollTop = Debug.debug_cmd_tooltip_text_div.scrollHeight;
    }

    static markCard(object_id: number, class_text='marked-card') {
        const card_div = Cards.getDiv(object_id)!
        if( card_div.classList.contains(class_text) ) {
            card_div.classList.remove(class_text)
        } else {
            document.querySelectorAll(`.${class_text}`).forEach(e => {
                e.classList.remove(class_text)
            })
            card_div.classList.add(class_text)
        }
    }

    static doEnter(debug_cmd: string) {
        if (typeof debug_cmd === "string") {
            if( debug_cmd.startsWith('0:') ) {
                const cmd = debug_cmd.replace('0:', 'save_0.json:')
                Button.doDebug(cmd, false);
            }
            else
            if (debug_cmd[0] === 'c' && Lib.string.isNumeric(debug_cmd.slice(1))) {
                const object_id = Number(debug_cmd.slice(1))
                if( Cards.getCard(object_id) ) {
                    Debug.setClickedCard(object_id)
                    const div = Cards.getDiv(object_id)
                    console.log(div)
                } else {
                    Lib.alert(`${object_id} not existed`)
                }
                // let num = Number(debug_cmd.slice(1));
                // Debug.markCard(num, 'debug-clicked')
            } else if (debug_cmd[0] === 'e' && Lib.string.isNumeric(debug_cmd.slice(1))) {
                let num = Number(debug_cmd.slice(1));
                let found = Cards.findObjectIdByEffectId(num)
                if( found ) {
                    Debug.setClickedCard(found)
                    // Debug.markCard(found, 'debug-clicked')
                }
            } else {
                Button.disablePause()
                function formatFunctionCall(input: string) {
                    input = input.replace(/,/g, ' ').trim();
                    let parts = input.split(/\s+/);
                    let functionName = parts[0];
                    if (functionName === "s") {
                        return input;
                    }
                    let params = parts.slice(1);
                    if (params[0].length <= 3 && Lib.string.isNumeric(params[0])) {
                        params[0] = `c${params[0]}`;
                    } else if (params[0].length === 5 && Lib.string.isNumeric(params[0])) {
                        params[0] = `'${params[0]}'`;
                    } else if (params[0].length === 6 && Lib.string.isNumeric(params[0].substring(0, 5))) {
                        params[0] = `'${params[0]}'`;
                    }
                    functionName = functionName.charAt(0).toUpperCase() + functionName.slice(1).toLowerCase();
                    if (functionName === 'Counter' || functionName == 'trait') {
                        if( !params[1].startsWith("\"") ) {
                            params[1] = `"${params[1]}"`;
                        }
                    }
                    else if (functionName === 'Putdeck') {
                        params[0] = `${params[0]}`;
                        functionName = 'PutIntoDeck'
                    }
                    else if (functionName === 'Putplay') {
                        params[0] = `${params[0]}`;
                        functionName = 'PutIntoPlay'
                    }
                    else if (functionName === 'Discarddeck') {
                        functionName = 'DiscardDeck'
                    }
                    else if (functionName === 'Cannot' ) {
                        functionName = 'CannotTarget'
                        return `${functionName}(${params[0]}, None, ${params.slice(1).join(',')})`;
                    }
                    else if (functionName === 'Can' ) {
                        functionName = 'CanTarget'
                        return `${functionName}(${params[0]}, None, ${params.slice(1).join(',')})`;
                    }
                    return `${functionName}(${params.join(',')})`;
                }

                let origin_debug_cmd = debug_cmd;
                if (debug_cmd.length === 5 && Lib.string.isNumeric(debug_cmd)) {
                    debug_cmd = `Gain("${debug_cmd}", "${debug_cmd}", "${debug_cmd}")`;
                } else if (!debug_cmd.startsWith("/") && !debug_cmd.includes('(') && debug_cmd.replaceAll(',', ' ').split(' ').length >= 2) {
                    debug_cmd = formatFunctionCall(debug_cmd);
                }

                Debug.last_all_inputs = Debug.last_all_inputs.filter(value => Lib.string.isStringANumber(value) || value.length >= 5);
                Debug.last_all_inputs.unshift(origin_debug_cmd);
                Debug.last_all_inputs = Debug.last_all_inputs.filter((value, index, array) => array.indexOf(value) === index);
                Debug.last_select_index = -1;
                Debug.last_all_inputs = Debug.last_all_inputs.slice(0, 50);

                if (Debug.last_all_inputs.length > 0) {
                    Lib.cookie.setList("debug_inputs", Debug.last_all_inputs)
                }
                Button.doDebug(debug_cmd, false);
            }
            Debug.debug_cmd_input_div.value = '';
            Debug.last_input = Lib.array.deepClone(Debug.last_all_inputs);
            Debug.last_help_tip = Lib.array.deepClone(Debug.help_tip);
        }
    }

    static onKeyDown(event: KeyboardEvent) {
        if (event.key === "Enter") {
            let debug_cmd = Debug.debug_cmd_input_div.value;

            Debug.doEnter(debug_cmd);
            Debug.debug_cmd_input_div.placeholder = debug_cmd;
            event.preventDefault();
        }
        else
        if (event.key === "Tab" && HistoryLog.isOpen()) {
            Button.doToggleHistory();
            event.preventDefault();
        } else if (event.key === "Tab") {
            Debug.debug_cmd_input_div.value = Debug.last_input[0];
            event.preventDefault();
        }

        else
        if (event.key === "Escape") {
            Debug.toggle();
            event.preventDefault();
        }

        else
        if (event.key === "Alt") {
            Debug.hideCmdTooltip();
            event.preventDefault();
        }

        else
        if (event.key === "ArrowUp" && (Debug.debug_cmd_input_div.selectionStart === 0 || document.activeElement != Debug.debug_cmd_input_div )) {
            Debug.debug_cmd_input_div.focus()
            Debug.last_select_index += 1;
            if (Debug.last_select_index >= Debug.last_input.length) {
                Debug.last_select_index = Debug.last_input.length - 1;
            }
            Debug.debug_cmd_input_div.value = Debug.last_input[Debug.last_select_index];
            Debug.updateDebugTooltip();
        }

        else
        if (event.key === "ArrowDown" && Debug.debug_cmd_input_div.selectionStart === Debug.debug_cmd_input_div.value.length) {
            Debug.last_select_index -= 1;
            if (Debug.last_select_index < 0) {
                Debug.last_select_index = 0;
            }
            Debug.debug_cmd_input_div.value = Debug.last_input[Debug.last_select_index];
            Debug.updateDebugTooltip();
        }

        else
        if (event.key === "`") {
            Debug.toggle();
            event.preventDefault();
        }
    }

    static onCardClick(div: HTMLElement, right: boolean) {
        let goto_click = true;
        const card_div = div;
        const object_id = Number(card_div.dataset.id!)

        if (Setting.is_debug) {
            goto_click = false;
            if (right) {
                if (Debug.right_menu_card !== card_div && Debug.right_menu_card !== card_div.parentElement && Debug.right_menu_card !== card_div.parentNode) {
                    Debug.right_menu_card.classList.remove('hide');
                    Debug.right_menu_card.style.left = `${WindowLoad.mouse_x + 5}px`;
                    Debug.right_menu_card.style.top = `${WindowLoad.mouse_y + 5}px`;

                    Lib.assert(Debug.right_menu_card != null, object_id)

                    Array.from(Debug.right_menu_card.children).forEach(button => {
                        button.classList.add('hide');
                        let func_str = button.getAttribute('data-function')
                        if( func_str ) {
                            const func = new Function('c', 'd', 'return ' + button.getAttribute('data-function'));
                            const card = Cards.getCard(object_id);
                            const deck = card_div.parentElement
                            if (func(card, deck)) {
                                button.classList.remove('hide');
                            }
                        }
                    });

                    if( Debug.right_menu_card.offsetTop + Debug.right_menu_card.clientHeight > window.innerHeight ) {
                        let top = window.innerHeight - Debug.right_menu_card.clientHeight
                        Debug.right_menu_card.style.top = `${top}px`;
                    }

                } else {
                    Debug.right_menu_card.classList.add('hide');
                }
                goto_click = true;
            } else {
                if (Debug.clicked_card_id !== object_id) {
                    goto_click = true;
                }
            }
        }
        else {
            if( right ) {
                if( HoverCard.hover_card ) {
                    const object_id = Number(HoverCard.hover_card.dataset.id!)
                    if( Cards.getCard(object_id)!.isVisible() || Setting.showAllCards() ) {
                        HoverCard.flip()
                        return false;
                    }
                }
            }
        }

        if (goto_click) {
            Debug.setClickedCard(object_id);
        }
    }
}

(window as any).Debug = Debug;

