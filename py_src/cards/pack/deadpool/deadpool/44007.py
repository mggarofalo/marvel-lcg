from . import *

# Montage

def GetAbilities() -> Sequence['Ability']:

    def montage(effect: 'Effect', message: 'Message.CheckPlayerCanPayCost') -> 'Resources|None':
        this = effect.this.CastTo(Resource)
        Unused(this)

        res = FacesCounter.GetPrintedResources([this])
        scheme = Worlds.FindMainScheme(this)
        if scheme:
            value = min(3, scheme.acceleration_token)
            res += Resources("G") * value
        return res

    return [
        AbilityFactory.CheckThisCanDropPay(
            montage
        ),
    ]

