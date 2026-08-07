from . import *

# Imitation Shape

def GetAbilities() -> Sequence['Ability']:

    def imitation_shape_interrupt(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.DoSchemeInstead(effect)

    def imitation_shape(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Hulkling"),
            thwart=2,
        ),
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            Enemy,
            imitation_shape_interrupt,
        ),
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.HeroResponse,
            "You",
            Event,
            imitation_shape
        ).SetCostFunc(CostFunc.Exhaust("This"))
    ]

