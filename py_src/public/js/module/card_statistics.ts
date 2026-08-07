// @ts-check
"use strict";

import { SetInfo } from './set_info.js';
import { Card } from './card_info.js';

class Statistics {
    resolve: number;
    play: number;
    defeated: number;
    is_new: boolean;

    constructor(resolve: number, play: number, defeated: number) {
        this.resolve = resolve;
        this.play = play;
        this.defeated = defeated;
        this.is_new = resolve === 0 && play === 0 && defeated === 0;
    }
}

export class CardStatistics {
    const_card_dict: { [key: string]: Card } = {};
    statistics_json: { [key: string]: Statistics } = {};
    cards_list: Card[] = [];
    cards_list_size: number;
    sets_json: { [key: string]: SetInfo } = {};

    constructor() {
        this.cards_list_size = 0;
    }

    getNotImplementedCards(package_name: string): string[] {
        let max_id = 0;
        for (const [key, set] of Object.entries(this.sets_json)) {
            if (set.name === package_name) {
                max_id = Number(set.max_id);
                break;
            }
        }

        if (max_id === 0) {
            return [];
        }

        const start_id = Math.floor(max_id / 1000) * 1000 + 1;
        const miss_cards: string[] = [];

        for (let i = start_id; i <= max_id; i++) {
            const i0 = i.toString().padStart(5, '0');
            const ia = i0 + 'a';
            const ib = i0 + 'b';

            if (!this.const_card_dict.hasOwnProperty(i0) && 
                !this.const_card_dict.hasOwnProperty(ia) && 
                !this.const_card_dict.hasOwnProperty(ib)) {
                miss_cards.push(i0);
            }
        }

        return miss_cards;
    }

    createCardObjects(data: { [key: string]: any }): number {
        this.cards_list = [];
        let size = 0;

        for (const packageName in data) {
            if (data.hasOwnProperty(packageName)) {
                const packageCards = data[packageName];
                // @ts-ignore
                packageCards.forEach(cardData => {
                    let card: Card;
                    if ('full_link' in cardData) {
                        const link_card = this.const_card_dict[cardData['full_link']];
                        card = new Card(
                            cardData.card_id,
                            link_card.type,
                            link_card.name,
                            link_card.subtitle,
                            link_card.desc,
                            link_card.set_name,
                            link_card.traits,
                            packageName,
                            link_card.card_id,
                            link_card.text,
                        );
                    } else {
                        let text = cardData.text;
                        if ('ability_link' in cardData) {
                            try {
                                text = this.const_card_dict[cardData['ability_link']].text;
                            } catch (error) {
                                text = "(error)";
                            }
                        }
                        if( text == undefined ) {
                            text = ""
                        }
                        card = new Card(
                            cardData.card_id,
                            cardData.type,
                            cardData.name,
                            cardData.subtitle,
                            cardData.desc,
                            'set_name' in cardData ? cardData.set_name : '',
                            'traits' in cardData ? cardData.traits : [],
                            packageName,
                            "",
                            text,
                        );
                    }
                    size += 1;
                    this.cards_list.push(card);
                    this.const_card_dict[cardData.card_id] = card;
                });
            }
            const card_ids = this.getNotImplementedCards(packageName);
            for (let card_id of card_ids) {
                const card = new Card(
                    card_id,
                    '',
                    'not implemented',
                    '',
                    // @ts-ignore
                    {},
                    '',
                    [],
                    packageName,
                    "",
                    ""
                );
                this.cards_list.push(card);
                this.const_card_dict[card_id] = card;
            }
        }
        return size;
    }

    async loadAllCards(load_statistics: boolean, load_set: boolean): Promise<void> {
        if (load_set) {
            const response_set = await fetch(`get_sets_json?`);
            if (!response_set.ok) {
                throw new Error('Network response was not ok: ' + response_set.statusText);
            }
            const set_raw_json = await response_set.json();
            for (let key in set_raw_json) {
                this.sets_json[key] = new SetInfo(set_raw_json[key]);
            }
        }

        const response_cards = await fetch(`get_cards_json?`);
        if (!response_cards.ok) {
            throw new Error('Network response was not ok: ' + response_cards.statusText);
        }
        const cards_json = await response_cards.json();
        this.cards_list_size = this.createCardObjects(cards_json);

        if (load_statistics) {
            const response_statistics = await fetch(`get_statistics?`);
            if (!response_statistics.ok) {
                throw new Error('Network response was not ok: ' + response_statistics.statusText);
            }
            const statistics_json_raw = await response_statistics.json();

            for (const key in statistics_json_raw) {
                this.statistics_json[key] = new Statistics(
                    statistics_json_raw[key][0],
                    statistics_json_raw[key][1],
                    statistics_json_raw[key][2],
                );
            }
        }
    }
}

