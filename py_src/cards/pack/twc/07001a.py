from . import *

# Breakout - 1A

def GetAbilities() -> Sequence['Ability']:

    def breakout(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        scheme_names = [
            DAY_OF_RECKONING,
            THUNDERSTRUCK,
            PILE_IT_ON,
            CLEAR_THE_ROAD,
        ]

        for name in scheme_names:
            SetupCards.PutIntoPlay(
                effect,
                name=name,
                card_type=SchemeSide2
            )

        # Place the active counter on Wrecker
        GetWrecker(effect).SetActive(effect)

    def scenario_prepare(effect: 'Effect', message: 'Message.WhenGameBeginSetup'):
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        card_ids = [
            [
                "07005",
                "07006",
                "07007",
                "07008",
                "07009",
                "07010",
                "07011",
                "07012",
                "07012",
                "07013",
                "07014",
                "07015",
                "07015",
                "07016",
                "07016",
            ],
            [
                "07020",
                "07021",
                "07022",
                "07023",
                "07024",
                "07025",
                "07026",
                "07027",
                "07027",
                "07028",
                "07029",
                "07030",
                "07030",
                "07031",
                "07031",
            ],
            [
                "07035",
                "07035",
                "07036",
                "07037",
                "07038",
                "07039",
                "07040",
                "07041",
                "07042",
                "07042",
                "07043",
                "07043",
                "07044",
                "07044",
                "07045",
            ],
            [
                "07049",
                "07050",
                "07051",
                "07051",
                "07052",
                "07053",
                "07054",
                "07055",
                "07055",
                "07056",
                "07057",
                "07058",
                "07058",
                "07059",
                "07059",
            ]
        ]

        def move_active_counter(effect: 'Effect', message: 'Message.WhenUnitBeDefeated'):
            villain = effect.this.CastTo(EncounterVillain)
            scheme = villain.FindCorrespondingScheme()
            if scheme:
                Faces.RemoveAllFromGame([scheme], effect)
                # Bug if there are attachment in play
                # Faces.RemoveAllFromGame(villain.encounter_deck.GetAll(), effect)
                if Worlds.FindVillain(effect) == effect.this:
                    scheme = GetSideSchemeWhoseHasSafe(effect, most_threat=True)
                    if scheme:
                        villain = scheme.FindCorrespondingVillain()
                        if villain:
                            villain.SetActive(effect)

        villain_names = [
            WRECKER,
            THUNDERBALL,
            PILEDRIVER,
            BULLDOZER
        ]

        first_player = Worlds.GetFirstPlayer(effect)
        scenario = Worlds.GetScenario(effect)

        message.CreateSetAside()
        encounter_decks = [
            GetThunderballEncounter(effect),
            GetPiledriverEncounter(effect),
            GetBulldozerEncounter(effect),
        ]

        for index in range(4):

            villain_name = villain_names[index]
            villain = Worlds.AsideDeck(effect).FindCard(name=villain_name, card_type=Villain)
            assert villain
            # villain.card.MoveToArea(scenario.villain_deck, effect)
            villain.PutIntoPlay(first_player, effect)

            encounetr_deck: Deck
            if index == 0:
                encounetr_deck = scenario.encounter_deck
                CardFactory.GenerateCards(card_ids[index], encounetr_deck, effect.world)
                Message.WhenDeckCreated_Text(encounetr_deck)
                encounetr_deck.Shuffle(effect)
            else:
                encounter_deck_type = encounter_decks[index-1]
                encounter_deck_type.Create(effect, villain, card_ids[index])
                encounetr_deck = encounter_deck_type.deck

            villain.SetEncounterDeck(encounetr_deck)

            villain.effect.Registers(
                AbilityFactory.WhenUnitBeDefeated(
                    AbilityType.WhenDefeated,
                    "This",
                    move_active_counter
                )
            )

        message.skip_create_encounter_deck = True
        message.no_use_obligation = True
        message.skip_put_villain_into_play = True

    return [
        AbilityFactory.WhenCardSetup(
            "This",
            breakout
        ),
        AbilityFactory.WhenGameBeginSetup(
            scenario_prepare
        ),
    ]

