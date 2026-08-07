from . import *

# Crisis Interdiction

def GetAbilities() -> Sequence['Ability']:

    def crisis_interdiction_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        villain = FindCaptainMarvel(effect)
        if villain:
            villain.DoSchemes(player, effect)

        PlaceEnergyCounterOnEnergyChannel(1, effect)

    def crisis_interdiction_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        this.PlaceThreatOnSchemes("EachScheme", 2, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            crisis_interdiction_revealed
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            crisis_interdiction_hero
        ),
    ]

