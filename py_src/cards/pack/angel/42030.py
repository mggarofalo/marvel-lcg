from . import *

# Eyes in the Sky

def GetAbilities() -> Sequence['Ability']:

    def eyes_in_the_sky(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        message.CancelAllEffectsAndDiscardIt(effect)

        Worlds.RevealEncounterDeckTopCard(initiator, effect)


    return [
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.HeroInterrupt,
            "You",
            NonEliteMinion,
            "All",
            eyes_in_the_sky,
        ).SetCostFunc(CostFunc.Exhaust("YouControlUnit", trait="AERIAL"))
        .SetCostFunc(CostFunc.Discard("This")),
    ]

