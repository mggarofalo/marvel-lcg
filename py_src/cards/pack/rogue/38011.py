from . import *

# * Karma: Xi'an Coy Manh

def GetAbilities() -> Sequence['Ability']:

    def karma(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        if effect.targets:
            minion = effect.targets[0].CastTo(Minion)

            Faces.TreatAsAlly(minion, "karma_controlled_ally", initiator, effect)


    return [
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            karma,
        ).SetTarget(Minion, non_trait="ELITE"),
    ]

