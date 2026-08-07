from . import *

# Smear Campaign

def GetAbilities() -> Sequence['Ability']:

    def smear_campaign_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        face = Worlds.FindCardOnField(effect, name="Public Outcry")
        if face:
            Faces.PlaceCountersOn([face], 2, 'notoriety', effect)
            Faces.RemoveAllFromGame([this], effect)
        else:
            this.PlaceThreatOnSchemes("MainScheme", 2, effect)

    return [
        AbilityFactory.IfExpertModeThisGainKeyword(
            surge=1
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            smear_campaign_revealed
        ),
    ]

