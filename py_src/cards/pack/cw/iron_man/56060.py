from . import *

# * Iron Man

def GetAbilities() -> Sequence['Ability']:

    def iron_man_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Leader)
        Unused(this)

        Utility.DealEachPlayerEncounterCard(effect)
        Utility.CannotTakeDamageThisPhase(this, effect)


    return [
        IronManGetsSCHForEachTechAttachmentOnHim(),
        AbilityFactory.WhenThisRevealed(
            None,
            iron_man_revealed
        ),
    ]

