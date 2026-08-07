from . import *

# * Web of Life and Destiny

def GetAbilities() -> Sequence['Ability']:

    def web_of_life_and_destiny(effect: 'Effect', message: 'Message.AfterCardLeavePlay') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        for target in effect.targets:
            player = target.GetControlByPlayer()
            player.DrawUp(1, effect)


    return [
        AbilityFactory.IgnoreThisCostIf(
            AbilityType.NonKeyword,
            your_identity_has_trait="WEB-WARRIOR",
        ),
        AbilityFactory.AfterCardLeavePlay(
            AbilityType.Response,
            Ally,
            web_of_life_and_destiny,
            conditions=[
                lambda effect, message:
                    message.trigger.HasTrait("WEB-WARRIOR"),
            ]
        ).SetTarget("Players"),
    ]

