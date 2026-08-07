from . import *

# Eyepatch Camera

def GetAbilities() -> Sequence['Ability']:

    def eyepatch_camera(effect: 'Effect', message: 'Message.WhenSchemeWouldPlaceThreat') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        message.value
        value = initiator.DeclareNumber(1, min(3, message.value))
        message.UpdateValue(-1 * value, effect)
        form = initiator.form.FindSuitForm()
        if form:
            Faces.PlaceTokensOn([form], value, 'threat', effect)


    return [
        AbilityFactory.WhenThreatWouldBePlacedOn(
            AbilityType.HeroInterrupt,
            MainScheme,
            eyepatch_camera
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

