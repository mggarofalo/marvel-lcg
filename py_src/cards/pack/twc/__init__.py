from cards.pack import *

DAY_OF_RECKONING = "Day of Reckoning"
THUNDERSTRUCK   = "Thunderstruck"
PILE_IT_ON      = "Pile It On!"
CLEAR_THE_ROAD  = "Clear the Road"

WRECKER     = "Wrecker"
THUNDERBALL = "Thunderball"
PILEDRIVER  = "Piledriver"
BULLDOZER   = "Bulldozer"

SCHEMES_VILLAINS: Dict[str, str] = {
    DAY_OF_RECKONING: WRECKER,
    THUNDERSTRUCK: THUNDERBALL,
    PILE_IT_ON: PILEDRIVER,
    CLEAR_THE_ROAD: BULLDOZER,
}

VILLAINS_SCHEMES: Dict[str, str] = {
    WRECKER: DAY_OF_RECKONING,
    THUNDERBALL: THUNDERSTRUCK,
    PILEDRIVER: PILE_IT_ON,
    BULLDOZER: CLEAR_THE_ROAD,
}

def GetWrecker(by_effect: 'Effect') -> EncounterVillain:
    villain = Worlds.FindCardOnField(by_effect, name=WRECKER, card_type=EncounterVillain)
    assert villain
    return villain

def GetThunderBall(by_effect: 'Effect') -> EncounterVillain:
    villain = Worlds.FindCardOnField(by_effect, name=THUNDERBALL, card_type=EncounterVillain)
    assert villain
    return villain

def GetPiledriver(by_effect: 'Effect') -> EncounterVillain:
    villain = Worlds.FindCardOnField(by_effect, name=PILEDRIVER, card_type=EncounterVillain)
    assert villain
    return villain

def GetBulldozer(by_effect: 'Effect') -> EncounterVillain:
    villain = Worlds.FindCardOnField(by_effect, name=BULLDOZER, card_type=EncounterVillain)
    assert villain
    return villain

################################################################################
#
def PlaceTheThreatOnHisSideScheme() -> 'Ability':

    def place_the_threat_on_his_side_scheme(effect: 'Effect') -> 'Scheme2|None':
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        scheme = this.FindCorrespondingScheme()
        return scheme

    return AbilityFactory.PlaceThreatOnCardWhenThisScheme(
        AbilityType.NonKeywordStar,
        place_the_threat_on_his_side_scheme
    )

def IfThereIsTenThreatThenRemoveAllButThree(operation: OperationType[Message.AfterSchemePlaceThreat]) -> 'Ability':
    def process(effect: 'Effect', message: 'Message.AfterSchemePlaceThreat'):
        this = effect.this.CastTo(EncounterSideScheme)

        operation(effect, message)

        value = this.threat - 3
        this.RemoveThreatFromSchemes([this], value, effect)
        if not effect.world.is_game_over:
            assert this.threat == 3, f"{this.threat=}"

    return AbilityFactory.AfterSchemePlaceThreat(
        AbilityType.ForcedResponse,
        "This",
        process,
        has_at_least_threat=10,
    )

################################################################################
#
# class WreckerEncounterDeck(SetAsideDeck):
#     pass

def GetThunderballEncounter(effect: 'Effect'):
    return Worlds.ScenarioDeck(effect, "ThunderballEncounter")

def GetPiledriverEncounter(effect: 'Effect'):
    return Worlds.ScenarioDeck(effect, "PiledriverEncounter")

def GetBulldozerEncounter(effect: 'Effect'):
    return Worlds.ScenarioDeck(effect, "BulldozerEncounter")

################################################################################
#
def GetSideSchemeWhoseHas(effect: 'Effect', *, least_threat: bool=False, most_threat: bool=False) -> 'EncounterSideScheme':
    scheme = GetSideSchemeWhoseHasSafe(effect, least_threat=least_threat, most_threat=most_threat)
    assert isinstance(scheme, EncounterSideScheme)
    return scheme

def GetSideSchemeWhoseHasSafe(effect: 'Effect', *, least_threat: bool=False, most_threat: bool=False) -> 'Scheme2|None':
    villains = Worlds.GetAllVillains(effect)
    schemes: List[Scheme2] = []
    for villain in villains:
        scheme = villain.FindCorrespondingScheme()
        if scheme:
            schemes.append(scheme)

    scheme = Filter.One(schemes, effect, least_threat=least_threat, most_threat=most_threat)
    return scheme
