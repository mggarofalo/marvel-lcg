export class HistoryLog
{
    static history_div? = document.getElementById("history") as HTMLElement;
    static history_text_div? = document.getElementById("history-text") as HTMLElement;
    static last_id = -1
    static history_text = ""
    static contextMenu = document.getElementById('contextMenu') as HTMLElement;
    static click_object_id = 0
    static click_card_id = ""
    static click_card_name = ""

    static {
        HistoryLog.history_div?.addEventListener('click', (e) => {
            let target = e.target as HTMLElement
            if ( target !== HistoryLog.contextMenu && !HistoryLog.contextMenu.contains(target)) {
                HistoryLog.reportMenuOff();
            }
        });
    }

    static close() {
        if( HistoryLog.isOpen() ) {
            HistoryLog.toggle()
        }
    }

    static isOpen(){
        return !HistoryLog.history_div?.classList.contains('hide')
    }
    static toggle() {
        HistoryLog.history_div?.classList.toggle('hide')
    }
    static addText(id: number, text: string) {
        if( id == HistoryLog.last_id ) {
            return
        }
        if( text && HistoryLog.history_text_div ) {
            text = text.replaceAll("\n", "")
            HistoryLog.last_id = id
            // text = `#${id.toString().padStart(3, '0').slice(-3)} ${text}`
            text = `<p class='log-id'>${id}</p> <p>${text}</p>`
            HistoryLog.history_text += text
            HistoryLog.history_text_div.innerHTML = HistoryLog.history_text // + `<div style="height: 220px;"></div>`
            // This cause more than 200ms to run
            // HistoryLog.history_text_div.scrollTop = HistoryLog.history_text_div.scrollHeight - HistoryLog.history_text_div.clientHeight;
        }
    }
    static isEmpty() {
        return HistoryLog.history_text_div?.innerHTML == ""
    }

    static showMenu(e: MouseEvent, object_id: number, card_id: string, card_name: string) {
        // e.preventDefault();
        HistoryLog.click_object_id = object_id
        HistoryLog.click_card_id = card_id
        HistoryLog.click_card_name = card_name
        HistoryLog.contextMenu.style.display = 'block';
        HistoryLog.contextMenu.style.left = `${e.pageX}px`;
        HistoryLog.contextMenu.style.top = `${e.pageY}px`;
        HistoryLog.contextMenu.querySelector('#view_in_mdb')!.innerHTML = card_id
    }

    static gotoMDB(e: HTMLElement) {
        HistoryLog.reportMenuOff()
        window.open(`https://marvelcdb.com/card/${HistoryLog.click_card_id}`)
    }

    // static editCard(e: HTMLElement) {
    //     HistoryLog.reportMenuOff()
    //     Debug.doDebugCmd(`edit ${HistoryLog.click_card_id}`)
    // }

    static markCard(e: HTMLElement) {
        HistoryLog.reportMenuOff()
        // Debug.markCard(HistoryLog.click_object_id, 'marked-card')
    }

    static reportMenuOff(e: HTMLElement|null=null) {
        HistoryLog.contextMenu.style.display = 'none';
    };
}

(window as any).HistoryLog = HistoryLog;
