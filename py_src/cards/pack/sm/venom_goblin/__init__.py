from cards.pack import *

def MoveGliderTo(player: 'Player', new_scheme: 'MainScheme', by_effect: 'Effect'):
    old_scheme = Worlds.FindCardOnField(
        by_effect,
        has_counter='glider',
        card_type=CanPlaceCounter,
    )
    this = by_effect.this
    Unused(this)

    if new_scheme != old_scheme:
        if old_scheme:
            Faces.RemoveCountersOn([old_scheme], 1, 'glider', by_effect)

        Faces.PlaceCountersOn([new_scheme], 1, 'glider', by_effect)

def MoveGliderToTheMainSchemeWithMostThreat(player: 'Player', by_effect: 'Effect') -> 'MainScheme':
    scheme = Filter.One(Worlds.GetMainSchemes(by_effect), by_effect, most_threat=True)
    assert scheme, f"{by_effect=}"

    MoveGliderTo(player, scheme, by_effect)

    return scheme

def MoveGliderToTheMainSchemeWithLeastThreat(player: 'Player', by_effect: 'Effect', acceleration: bool) -> 'MainScheme':
    scheme = Filter.One(Worlds.GetMainSchemes(by_effect), by_effect, least_threat=True)
    assert scheme, f"{by_effect=}"

    MoveGliderTo(player, scheme, by_effect)

    return scheme

def LoseTheGameIfAtLeast2SymbioteEnviromentsInPlay() -> 'Ability':

    def manhattan(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        if Worlds.FindCardSizeOnField(effect, CardFinder2("SYMBIOTE", Environment)) >= 2:
            Worlds.SetGameOver(False, effect)

    return AbilityFactory.AfterCardEnterPlay(
        AbilityType.NonKeywordBold,
        "This",
        manhattan
    )

