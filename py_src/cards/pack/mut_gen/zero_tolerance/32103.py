from . import *

# Energy Barrier

def GetAbilities() -> Sequence['Ability']:

    def energy_barrier(face: 'CardFace', effect: 'Effect') -> None:
        Faces.GiveStatus([face], "Tough", effect)

    def energy_barrier_tough(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.GiveStatus([message.attacker], "Tough", effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder2("SENTINEL", Minion),
            without_another_copy=True,
            when_attach_operation=energy_barrier,
            if_cannot_gain_surge=True
        ),
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.ForcedResponse,
            "AttachedMinion",
            energy_barrier_tough
        ),
    ]

