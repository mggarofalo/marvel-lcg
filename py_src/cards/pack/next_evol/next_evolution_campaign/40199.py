from . import *

# * Malice

def GetAbilities() -> Sequence['Ability']:

    def malice(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        faces = Worlds.GetOnFieldAllies(effect, CardFinder(non_trait="PSIONIC"))
        face = Filter.One(faces, effect, highest_cost=True)
        if face:
            this.AttachTo2(face, effect) 


    return [
        AbilityFactory.TreatAttachedCardAsMinion(
            Ally,
            "Possessed Minion",
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            malice
        ),
    ]

