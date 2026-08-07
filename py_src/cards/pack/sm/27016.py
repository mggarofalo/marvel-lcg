from . import *

# What Doesn't Kill Me

def GetAbilities() -> Sequence['Ability']:

    def what_doesnt_kill_me(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        # if you cannot resolve the ability on What Doesn’t Kill me, readying your hero, you are also unable to pay the card’s cost, healing 2
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            what_doesnt_kill_me
        ).SetPlay().SetLabel()
        .SetTarget("YourHero", canbe_ready=True)
        .SetCostFunc(CostFunc.Heal(2, "YourHero")),
    ]

