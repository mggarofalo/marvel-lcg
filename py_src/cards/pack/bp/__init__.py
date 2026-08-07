from cards.pack import *

def AfterThisUsesBasicPowerResolveSpecialOnAnotherDoraMilajeAlly():

    def aneka(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.ResolveSpecialAbility(effect.targets, effect)

    return AbilityFactory.AfterUnitUseBasicPower(
        AbilityType.Response,
        "This",
        aneka
    ).SetTarget(Ally, trait="DORA MILAJE", exclude="This")

