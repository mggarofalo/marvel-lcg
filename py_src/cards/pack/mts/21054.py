from . import *

# Eternity

def GetAbilities() -> Sequence['Ability']:

    def eternity(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ShuffleAllTo([this], "EncounterDeck", effect)

    def eternity_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        player = message.GetToPlayer()
        player.DrawUp(1, effect)
        Faces.RemoveAllFromGame([this], effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            eternity
        ).SetPlay().SetLabel(),
        AbilityFactory.WhenThisRevealed(
            None,
            eternity_revealed
        ).SetCannotBeCancel()
    ]

