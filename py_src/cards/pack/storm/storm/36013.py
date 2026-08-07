from . import *

# Blast of Wind

def GetAbilities() -> Sequence['Ability']:

    def blast_of_wind(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 3, effect)

        initiator = effect.GetInitiator()
        weather = FindYourWeatherSupport(initiator)
        if weather:
            weather.ResolveSpecialAbility(initiator, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            blast_of_wind
        ).SetPlay().SetLabel()
        .SetTarget("VillainAndEngagedSamePlayerMinion"),
    ]

