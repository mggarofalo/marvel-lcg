from . import *

# Possessed

def GetAbilities() -> Sequence['Ability']:

    def possessed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        faces = Worlds.GetOnFieldAllies(effect, CardFinder(not_with_attach=CardFinder(name="Possessed")))
        ally = Filter.One(faces, effect, lowest_thw=True)
        if ally:
            this.AttachTo2(ally, effect)
        else:
            ThisCardGainSurge(effect)


    return [
        AbilityFactory.TreatAttachedCardAsMinion(
            Ally,
            "Controlled Minion",
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            possessed
        ),
    ]

