from . import *

# "Lost" Child

def GetAbilities() -> Sequence['Ability']:

    def lost_child(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        allies = Worlds.GetOnFieldAllies(
            effect,
            CardFinder(
                not_with_attach=CardFinder(name='"Lost" Child')
            )
        )
        unit = Filter.One(allies, effect, highest_cost=True)
        if unit:
            this.AttachTo2(unit, effect)
        else:
            ThisCardGainSurge(effect)

    return [
        AbilityFactory.TreatAttachedCardAsMinion(
            Ally,
            "Regressed Minion",
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            lost_child
        ),
    ]

