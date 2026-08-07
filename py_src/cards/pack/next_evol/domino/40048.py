from . import *

# Lucky Break

def GetAbilities() -> Sequence['Ability']:

    def lucky_break(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        message.CancelAllEffectsAndDiscardIt(effect)

        Worlds.RevealEncounterDeckTopCard(initiator, effect)

    return [
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.HeroInterrupt,
            "You",
            EncounterCard,
            "All",
            lucky_break
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

