from . import *

# Fearless Determination

def GetAbilities() -> Sequence['Ability']:

    def fearless_determination(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = effect.targets[0]
        hero.GainUntilPhaseEnd(effect, thwart=1)
        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            fearless_determination
        ).SetPlay()
        .SetTarget("YourHero")
    ]
