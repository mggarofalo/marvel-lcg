from cards.pack import *

def AttackOnXaviers() -> List['Ability']:

    def attack_on_xaviers_completed(effect: 'Effect', message: 'Message.WhenMainSchemeStageCompleted') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        Faces.AddToVictoryDisplay([this], effect)

    def attack_on_xaviers(effect: 'Effect', message: 'Message2') -> None:
        if Worlds.VictoryDisplay(effect).FindCardSize(CardFinder(card_type=MainScheme)) >= 3:
            Worlds.SetGameOver(False, effect)

    return [
        AbilityFactory.WhenMainSchemeStageCompleted(
            AbilityType.WhenCompleted,
            "This",
            attack_on_xaviers_completed
        ),
        AbilityFactory.WhenCardRevealed(
            AbilityType.NonKeywordBold,
            "This",
            attack_on_xaviers
        )
    ]

def GroundSwell(name: str) -> List['Ability']:

    def ground_swell_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        unit = Worlds.FindCardOnField(
            effect,
            name=name,
            card_type=Enemy
        )
        if unit:
            unit.DoActivate(player, effect)
        else:
            unit = Search.EncounterCard(
                effect,
                include_discard_pile=True,
                name=name,
                card_type=Minion
            )
            if unit:
                unit.Reveal(None, effect)

    def ground_swell_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        if villain and villain.IsName(name):
            message.GiveBoostCardForThisActivation(Villain, 1, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            ground_swell_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            ground_swell_boost
        ),
    ]

