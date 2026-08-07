from . import *

# The Gardener

def GetAbilities() -> Sequence['Ability']:

    def the_gardener(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ShuffleAllTo([this], "EncounterDeck", effect)

    def the_gardener_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        player = message.GetToPlayer()
        this.HealthUnits([player.GetIdentity()], 2, effect)
        Faces.RemoveAllFromGame([this], effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            the_gardener
        ).SetPlay().SetLabel(),
        AbilityFactory.WhenThisRevealed(
            None,
            the_gardener_revealed
        ).SetCannotBeCancel()
    ]

