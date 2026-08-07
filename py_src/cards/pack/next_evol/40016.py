from . import *

# * Sunspot: Bobby Da Costa

def GetAbilities() -> Sequence['Ability']:

    def sunspot(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        damage = message.paid_resources.GetColor("Y")
        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            sunspot,
        ).SetTarget("VillainAndEngagedSamePlayerMinion"),
    ]

