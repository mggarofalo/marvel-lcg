from . import *

# * Tempus: Eva Bell

def GetAbilities() -> Sequence['Ability']:

    def tempus(effect: 'Effect', message: 'Message.WhenUnitWouldScheme') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        message.SetBeInstead(effect)
        initiator = effect.GetInitiator()
        initiator.DealEncounterCards(1, effect)


    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_your_identity_has_trait="X-MEN"),
        AbilityFactory.WhenUnitWouldScheme(
            AbilityType.Interrupt,
            Villain,
            tempus
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

