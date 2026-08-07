from . import *

# SP//dr Command

def GetAbilities() -> Sequence['Ability']:

    def spdr_command(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)

    def spdr_command2(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            spdr_command
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Exhaust(card_type=Upgrade, trait="INTERFACE")),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            spdr_command2
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Discard("YourHandCards"))
        .SetTarget(Upgrade, trait="INTERFACE", canbe_ready=True),
    ]

