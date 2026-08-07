from . import *

# Defensive Energy

def GetAbilities() -> Sequence['Ability']:

    def defensive_energy(effect: 'Effect', message: 'Message.WhenCardBeSpendAsResource') -> None:
        this = effect.this.CastTo(Resource)
        Unused(this)

        for target in effect.targets:
            player = target.GetControlByPlayer()
            player.DrawUp(1, effect)


    return [
        AbilityFactory.WhenYouSpendThisCardToPlay(
            AbilityType.HeroResponse,
            defensive_energy,
            CardFinder2("DEFENSE", Event),
        ).SetTarget("Initiator"),
    ]

