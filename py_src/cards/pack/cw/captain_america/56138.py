from . import *

# * Captain America

def GetAbilities() -> Sequence['Ability']:

    def captain_america_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Leader)
        Unused(this)

        Utility.DealEachPlayerEncounterCard(effect)
        Utility.CannotTakeDamageThisPhase(this, effect)


    def captain_america(effect: 'Effect', message: 'Message.AfterCardAttachTo') -> None:
        this = effect.this.CastTo(Leader)
        Unused(this)

        Faces.GiveStatus([this], "Tough", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            captain_america_revealed
        ),
        AbilityFactory.AfterCardAttachTo(
            AbilityType.ForcedResponse,
            CardFinder(name="Cap's Shield"),
            "This",
            captain_america
        ),
    ]

