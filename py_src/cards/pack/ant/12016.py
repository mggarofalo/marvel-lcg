from . import *

# Moxie

def GetAbilities() -> Sequence['Ability']:

    def moxie(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()
        hero.GainUntilRoundEnd(effect, attack=1, thwart=1, defense=1)


    return [
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.HeroResponse,
            "You",
            moxie,
        ).SetPlay()
    ]

