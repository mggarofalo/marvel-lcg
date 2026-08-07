from . import *

# * Jubilee

def GetAbilities() -> Sequence['Ability']:

    def jubilee(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        target = effect.targets[0]

        this.effect.RegisterTemp(
            AbilityFactory.WhenUnitWouldAttack(
                AbilityType.Temp0,
                CardFinder(names=["Wolverine", "Jubilee"]),
                lambda effect, message:
                    message.GainATKForThisAttack(2, effect),
                is_basic_attack=True,
                attack_targets=target
            ),
            unregister_after_exec=False,
            until_phase_end=True
        )


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            jubilee
        ).SetTarget(Enemy),
    ]

