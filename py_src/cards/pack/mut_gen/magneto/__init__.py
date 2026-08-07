from cards.pack import *

def AfterMagnetoAttacksYouPlace1MagnetCounterOnTheMainScheme():
    def magneto(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        scheme = Worlds.FindMainScheme(effect)
        if scheme:
            Faces.PlaceCountersOn([scheme], 1, 'magnet', effect)

    return AbilityFactory.AfterUnitAttackEnd(
        AbilityType.ForcedResponse,
        "This",
        magneto,
        against_who="You"
    )

