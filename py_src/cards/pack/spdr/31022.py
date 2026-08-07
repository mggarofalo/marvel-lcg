from . import *

# * Spider-Man: Otto Octavius

def GetAbilities() -> Sequence['Ability']:

    def spider_man(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        Faces.ReadyAll(effect.targets, effect)
        for target in effect.targets:
            if target.HasTrait("TECH"):
                initiator.DrawUp(1, effect)
                break


    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_you_control_trait="WEB-WARRIOR"),
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            spider_man,
        ).SetTarget(Upgrade, canbe_ready=True, from_where=["YouControlCards"])
    ]

