from . import *
from cards.pack.trors.campaign import *

# The Rise of Red Skull

def GetAbilities() -> Sequence['Ability']:

    def the_rise_of_the_red_skull(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        red_house = SetupCards.PutIntoPlay(
            effect,
            name="The Red House",
            card_type=EncounterSideScheme
        )
        if not red_house:
            red_houses = []
        else:
            red_houses = [red_house]

        faces: List[EncounterSideScheme] = []
        for face in Worlds.AsideDeck(effect).Get(True) + Worlds.GetEncounterDeckCards(effect):
            if EncounterSideScheme.IsType(face):
                faces.append(face)

        villain = Worlds.FindVillain(effect)
        assert villain
        GetSideSchemeDeck(effect).Create(effect, villain)
        Faces.ShuffleAllTo(faces, GetSideSchemeDeck(effect).deck, effect)

        Faces.BindDiscardPile(red_houses + faces, GetSideSchemeDeck(effect).discard_pile)

    def the_rise_of_the_red_skull_place_threat(effect: 'Effect', message: 'Message.WhenCampaignSetup') -> None:
        this = effect.this.CastTo(MainScheme)
        value = CampaignLog.GetInt("Number of delay counters on main scheme", effect)
        if Worlds.IsExpert(effect):
            value *= Worlds.GetPlayerNumIcon(effect)
        this.PlaceThreatOnSchemes("MainScheme", value, effect)

    def the_rise_of_the_red_skull_deal_encounter_card(effect: 'Effect', message: 'Message.WhenCampaignSetup') -> None:
        player_ids = [int(x) for x in CampaignLog.GetList("Players engaged with minions", effect) if x != ""]
        for player in Worlds.GetPlayers(effect):
            if player.player_id in player_ids:
                player.DealEncounterCards(1, effect)

    def the_rise_of_the_red_skull_remove_allies(effect: 'Effect', message: 'Message.WhenCampaignSetup') -> None:
        ally_ids = CampaignLog.GetList("Allies removed from the campaign", effect)

        if not ally_ids:
            return

        allies: List['CardFace'] = []
        for player in Worlds.GetPlayers(effect):
            allies += player.player_deck.FindCards(card_type=Ally, card_ids=ally_ids)
        Faces.FlipAllTo(allies, True, effect)
        Faces.RemoveAllFromGame(allies, effect)

    return [
        *CampaignSetup(5, campaigns=[
            the_rise_of_the_red_skull_place_threat,
            the_rise_of_the_red_skull_remove_allies
        ], campaigns_expert=[
            the_rise_of_the_red_skull_deal_encounter_card
        ]),
        AbilityFactory.WhenCardSetup(
            "This",
            the_rise_of_the_red_skull
        ),
    ]

