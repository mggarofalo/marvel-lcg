from . import *

# Beguiled

def GetAbilities() -> Sequence['Ability']:

    def beguiled_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        allies = Worlds.GetOnFieldAllies(effect, CardFinder(not_with_attach=CardFinder(name="Beguiled")))
        unit = Filter.One(allies, effect, highest_cost=True)
        if unit:
            this.AttachTo2(unit, effect)
        else:
            ThisCardGainSurge(effect)


    return [
        AbilityFactory.TreatAttachedCardAsMinion(
            Ally,
            "Enthralled Minion",
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            beguiled_revealed
        ),
    ]

