from . import *

# Top Billing

def GetAbilities() -> Sequence['Ability']:

    def top_billing_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        faces = player.GetControlCharacters()
        this.HealthUnits(faces, 1, effect)
        Faces.PlaceTokensOn(faces, 2, 'threat', effect)

    def top_billing_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        faces = player.GetControlCharacters()
        Faces.PlaceTokensOn(faces, 1, 'threat', effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            top_billing_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            top_billing_boost
        ),
    ]

