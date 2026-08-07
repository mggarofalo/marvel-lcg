from . import *

# Citywide Crisis

def GetAbilities() -> Sequence['Ability']:

    def citywide_crisis_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        schemes = Worlds.GetSideSchemes(effect)
        if not Faces.ResolveAbility(schemes, player, AbilityType.WhenRevealed, effect):
            this.PlaceThreatOnSchemes("EachScheme", 2, effect)


    def citywide_crisis_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        Faces.ResolveAbility([this], player, AbilityType.WhenRevealed, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            citywide_crisis_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            citywide_crisis_boost
        ),
    ]

