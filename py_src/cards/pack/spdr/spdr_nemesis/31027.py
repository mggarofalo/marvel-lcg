from . import *

# M.O.R.B.I.U.S.

def GetAbilities() -> Sequence['Ability']:

    def morbius(effect: 'Effect', message: 'Message.WhenCardBeSpendAsResource') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        value = message.res.val
        this.DealDamage([identity], value, effect)

    return [
        AbilityFactory.AfterGenerateResources(
            AbilityType.ForcedInterrupt,
            "EngagedPlayer",
            morbius,
            conditions=[
                lambda effect, message:
                    message.GetToPlayer().IsHero(),
            ]
        ),
    ]

