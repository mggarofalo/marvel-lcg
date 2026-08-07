from . import *

# Vapors of Valtorr

def GetAbilities() -> Sequence['Ability']:

    def vapors_of_valtorr(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        for target in effect.targets:
            face = target.GetBindFace().CastTo(Unit2)
            status_card = target.CastTo(StatusCard)
            status_target = status_card.GetBindFace()

            select_status = initiator.DeclareStatusCard(
                confused=face.CanbeConfused(),
                tough=face.CanGainTough(),
                stunned=face.CanbeStunned()
            )

            if select_status:
                if Faces.GiveStatus([status_target], select_status, effect):
                    Faces.DiscardAll([status_card], effect)

        PlaceThisCardInInvocationDeckDiscardPile(this, effect)


    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            vapors_of_valtorr
        ).SetTarget(StatusCard, range=(0, 1))
        .NoOutOfPlayLimit()
    ]

