from . import *

# "I Don't Think So!"

def GetAbilities() -> Sequence['Ability']:

    def i_dont_think_so(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        SayIDontThinkSoInYourBestSpiderHamVoice()
        message.CancelAllEffectsAndDiscardIt(effect)


    return [
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.HeroInterrupt,
            "You",
            None,
            "All",
            i_dont_think_so,
            from_encounter=True,
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Counter(Select.From("YourIdentity"), 1, 'toon'))
    ]

