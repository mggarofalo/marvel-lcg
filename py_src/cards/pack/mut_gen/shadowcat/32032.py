from . import *

# * Lockheed

def GetAbilities() -> Sequence['Ability']:

    def lockheed(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        if initiator.form.IsInMassForm("Solid"):
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.DealDamage(targets, 2, effect)
                ).SetTarget(Enemy)
            )
        elif initiator.form.IsInMassForm("Phased"):
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.RemoveThreatFromSchemes(targets, 2, effect)
                ).SetTarget(Scheme2)
            )


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            lockheed,
            # condition=lambda effect, message:
            #     Condition.PlayerInMassForm("You", "Solid", effect)
        ),
    ]

