from . import *

# Magnetically Sealed

def GetAbilities() -> Sequence['Ability']:

    def magnetically_sealed_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        allies = Worlds.GetOnFieldAllies(effect)
        value = 2 * len(allies)

        this.PlaceThreatOnSchemes([this], value, effect)

    def magnetically_sealed_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()

        faces = player.GetControlAllies()
        Faces.ExhaustAll(faces, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            magnetically_sealed_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            magnetically_sealed_boost
        ),
    ]

