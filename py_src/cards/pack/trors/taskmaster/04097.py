from . import *

# * Moon Knight: Marc Spector

def GetAbilities() -> Sequence['Ability']:

    def moon_knight(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        player = effect.GetInitiator()
        player.DrawUp(2, effect)


    return [
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            moon_knight,
        ).SetCost(Cost("G")),
    ]

