from . import *

# Living Tribunal

def GetAbilities() -> Sequence['Ability']:

    def living_tribunal(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ShuffleAllTo([this], "EncounterDeck", effect)
    
    def living_tribunal_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.RemoveThreatFromSchemes("MainScheme", 2, effect)

        Faces.RemoveAllFromGame([this], effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            living_tribunal
        ).SetPlay().SetLabel(),
        AbilityFactory.WhenThisRevealed(
            None,
            living_tribunal_revealed
        ).SetLabel()
        .SetCannotBeCancel(),
    ]

