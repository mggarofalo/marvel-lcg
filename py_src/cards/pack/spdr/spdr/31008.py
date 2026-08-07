from . import *

# Ejection Protocol

def GetAbilities() -> Sequence['Ability']:

    def ejection_protocol(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()

        faces = initiator.GetControlCardsByType(CardFinder2("INTERFACE"), upgrade=True)
        Faces.ExhaustAll(faces, effect)

        identity = initiator.GetIdentity()
        identity.SetHealth(6, effect)
        Faces.GiveStatus([identity], "Tough", effect)

        identity.ChangeToForm(AlterEgo, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            ejection_protocol
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

