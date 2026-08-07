from . import *

# * Dum Dum Dugan

def GetAbilities() -> Sequence['Ability']:

    def dum_dum_dugan(effect: 'Effect', message: 'Message.WhenUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        faces = Faces.ExhaustAll(effect.targets, effect)
        value = len(faces)
        message.GainValue(value, effect)


    return [
        AbilityFactory.WhenUnitUseBasicPower(
            AbilityType.Interrupt,
            "This",
            dum_dum_dugan
        ).SetTarget(trait="S.H.I.E.L.D", canbe_exhaust=True,
            range=(1, 3), from_where=["YouControlCards"]),
    ]

