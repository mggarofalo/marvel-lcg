from . import *

# Impervious

def GetAbilities() -> Sequence['Ability']:

    def impervious_discard(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        villain = Worlds.FindVillain(effect)

        if villain:
            Faces.GiveStatus([villain], "Tough", effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        AbilityFactory.ReduceCharacterTakeDamageBy(
            Villain,
            1,
            this_attached_unit_only=True,
            from_each_attack=True
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
            ex_operation=impervious_discard
        ).SetCost(Cost("RR")),
    ]

