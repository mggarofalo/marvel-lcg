from . import *

# * Moon Girl: Lunella Lafayette

def GetAbilities() -> Sequence['Ability']:

    def moon_girl(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        for target in effect.targets:
            player = target.GetControlByPlayer()
            value = message.paid_resources.GetColor("B")
            player.DrawUp(value, effect)


    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_your_identity_has_one_of_traits=["CHAMPION", "GENIUS"]),
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            moon_girl,
        ).SetTarget("Initiator"),
    ]

