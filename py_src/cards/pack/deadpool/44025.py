from . import *

# Self Confidence

def GetAbilities() -> Sequence['Ability']:

    def self_confidence(effect: 'Effect', message: 'Message.CheckPlayerCanPayCost') -> 'Resources|None':
        this = effect.this.CastTo(Resource)
        Unused(this)

        initiator = effect.GetInitiator()

        res = FacesCounter.GetPrintedResources([this])
        if initiator.GetIdentity().sustained == 0:
            res *= 3
        elif initiator.GetIdentity().sustained < 5:
            res *= 2
        return res

    return [
        AbilityFactory.CheckThisCanDropPay(
            self_confidence
        ),
    ]

