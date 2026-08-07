from cards.pack import *

THE_CHRONOPOLIS = "The Chronopolis"
INEXORABLE_FATE = "Inexorable Fate"
THE_REALM_OF_RAMA_TUT = "The Realm of Rama-Tut"
THE_PRESENT_FUTURE_WAR = "The Present Future War"

KANGS_DOMINION = "Kang's Dominion"

KANG_IMMORTUS = "Kang (Immortus)"
KANG_IRON_LAD = "Kang (Iron Lad)"
KANG_RAMA_TUT = "Kang (Rama-Tut)"
KANG_SCARLET_CENTURION = "Kang (Scarlet Centurion)"

def JoinOtherGameArea(game_area: 'GameArea', by_effect: 'Effect'):

    players = by_effect.world.const_players[:]

    for player in game_area.FindAllPlayers():
        players.remove(player)

    if players != []:
        for player in game_area.FindAllPlayers():
            player.ChooseAbilities(
                by_effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        targets[0].card.GetGameArea().AddPlayer(player)
                ).SetTarget([x.GetIdentity() for x in players])
            )
    else:
        assert len(by_effect.world.area_schemes_main.Get()) == 1, f"{by_effect.world.area_schemes_main=}"
        scheme = by_effect.world.area_schemes_main.Get()[0]
        for player in game_area.FindAllPlayers():
            by_effect.world.GetFirstGameArea().AddPlayer(player)
        assert scheme.IsName("The Master of Time"), f"{scheme=}"
        scheme.Advance(None, by_effect)

def WhenDefeatedRemoveSchemeAndJoinAnotherGameArea(scheme_name: str):
    def remove_scheme_and_join(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        scheme = Worlds.FindCardOnField(
            effect,
            name=scheme_name,
            card_type=MainScheme
        )
        assert scheme
        Faces.RemoveAllFromGame([scheme], effect)

        game_area = this.card.GetGameArea()

        Faces.RemoveAllFromGame([this], effect)

        def join_other_game_area():
            JoinOtherGameArea(game_area, effect)

        RunAt.TheEndOfThePhase(effect, join_other_game_area)

    return AbilityFactory.WhenUnitBeDefeated(
        AbilityType.WhenDefeated,
        "This",
        remove_scheme_and_join
    )

def KangTheConquerorAttack() -> 'Ability':
    def kang_the_conqueror_i(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        for target in message.attacked_targets:
            player = target.GetControlByPlayer()

            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Place 1 threat on the main scheme",
                    lambda targets:
                        this.PlaceThreatOnSchemes(targets, 1, effect)
                ).SetTarget(MainScheme, can_place_threat=True),
                AbilityFactory.ForChoiceAbility(
                    "He gets +2 ATK for this attack",
                    lambda targets:
                        this.GainForThisActive(effect, message, attack=2)
                ).SetTarget([this]),
            )

    return AbilityFactory.WhenUnitAttackYou(
        AbilityType.ForcedInterrupt,
        "This",
        kang_the_conqueror_i,
    )

def KangMainSchemStage3Reveal(kang_name: str) -> 'Ability':
    def main_scheme_stage3(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        kang = Worlds.AsideDeck(effect).FindCard(name=kang_name, card_type=Villain)
        assert kang
        player = message.GetToPlayer()
        kang.PutIntoPlay(player, effect)
        kang.Reveal(player, effect)

        game_area = effect.world.CreateGameArea()

        with message.GetToPlayer() as player:
            game_area.AddPlayer(player)
            game_area.AddCard(this.card)
            game_area.AddCard(kang.card)
            player.DealEncounterCards(1, effect)

    return AbilityFactory.WhenThisRevealed(
        None,
        main_scheme_stage3
    )

def KangMainSchemStage3Completed(kang_name: str) -> 'Ability':
    def kang_stage_completed(effect: 'Effect', message: 'Message.AfterMainSchemeCompleted') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        face = Worlds.AsideDeck(effect).FindCard(name=KANGS_DOMINION, card_type=SchemeSide2)
        assert face
        scheme = Worlds.MainSchemesDeck(effect).FindCard(name="Kang's Wrath", card_type=MainScheme)
        assert scheme
        scheme.PlaceCardHere(face, False, effect)

        def remove_kang_and_this():
            kang = Worlds.FindCardOnField(
                effect,
                name=kang_name,
                card_type=Villain
            )
            assert kang

            game_area = this.card.GetGameArea()
            Faces.RemoveAllFromGame([kang, this], effect)

            JoinOtherGameArea(game_area, effect)

            # CombineYourGameAreaWithAnotherGameArea()

            # TODO: If all the players at this stage are defeated, this stage is complete.

        RunAt.TheEndOfThePhase(effect, remove_kang_and_this)

    return AbilityFactory.AfterMainSchemeCompleted(
        AbilityType.ForcedResponse,
        "This",
        kang_stage_completed
    )

