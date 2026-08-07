from . import *

# Joy Ride

def GetAbilities() -> Sequence['Ability']:

    def joy_ride(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()

        scheme = MoveGliderToTheMainSchemeWithMostThreat(player, effect)
        scheme.ResolveSpecialAbility(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            joy_ride
        )
    ]

