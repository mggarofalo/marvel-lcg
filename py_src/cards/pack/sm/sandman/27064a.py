from . import *
from cards.pack.sm.campaign import *

# Hapless Pedestrians

def GetAbilities() -> Sequence['Ability']:

    def hapless_pedestrians(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        SetupCards.PutIntoPlay(
            effect,
            name="City Streets",
            card_type=Environment
        )

        city_streets = GetCityStreets(effect)
        if city_streets:
            Faces.PlaceCountersOn([city_streets], 4, 'sand', effect)

    def campaign_expert(effect: 'Effect', message: 'Message.WhenCampaignSetup'):
        face = Worlds.FindCardOnField(effect, name="City Streets", card_type=Environment)
        if face:
            Faces.PlaceCountersOn([face], 2, 'sand', effect)
            face.ResolveSpecialAbility(None, effect, name=SURGING_SANDS)

    return [
        *CampaignSetup(1, campaign_expert=campaign_expert),
        AbilityFactory.WhenCardSetup(
            "This",
            hapless_pedestrians
        ),
    ]

