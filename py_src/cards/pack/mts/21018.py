from . import *

# Band Together

def GetAbilities() -> Sequence['Ability']:

    def band_together(effect: 'Effect', message: 'Message.CheckPlayerCanPayCost') -> 'Resources|None':
        this = effect.this.CastTo(Resource)
        Unused(this)

        initiator = effect.GetInitiator()
        res = Resources("G")
        return res * min(3, len(initiator.GetControlAllies()))

    return [
        AbilityFactory.CheckThisCanDropPay(
            band_together
        ),
    ]

