from . import *

# * Spider-Man: Pavitr Prabhakar

def GetAbilities() -> Sequence['Ability']:

    def spider_man(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        value = len(initiator.GetControlCards2(CardFinder2("WEB-WARRIOR")))
        this.RemoveThreatFromSchemes(effect.targets, value, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            spider_man
        ).SetTarget(Scheme2),
    ]

