from . import *

# Natural Agility

def GetAbilities() -> Sequence['Ability']:

    def natural_agility(effect: 'Effect', message: 'Message.WhenUnitWouldDefend') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        value = initiator.GetIdentity().GetCounters('charge')

        message.GainDEFForThisAttack(
            value,
            effect
        )


    return [
        AbilityFactory.WhenUnitDefendAgainstAttack(
            AbilityType.HeroInterrupt,
            "You",
            natural_agility
        ).SetPlay().SetLabel('defense')
        .SetCostFunc(CostFunc.PlaceCounter(CardFinder(name="Gambit"), 1, 'charge')),
    ]

