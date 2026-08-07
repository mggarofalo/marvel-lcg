from . import *

# 'Pool-ized

def GetAbilities() -> Sequence['Ability']:

    def pool_ized(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        allies = Worlds.GetOnFieldAllies(effect, CardFinder(not_with_attach=CardFinder(name="'Pool-ized")))
        ally = Filter.One(allies, effect, highest_cost=True)
        if ally:
            this.AttachTo2(ally, effect)
        else:
            ThisCardGainSurge(effect)


    return [
        AbilityFactory.TreatAttachedCardAsMinion(
            Ally,
            "'Pool Minion"
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            pool_ized
        ),
    ]

