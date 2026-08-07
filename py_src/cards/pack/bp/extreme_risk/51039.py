from . import *

# * Joystick

def GetAbilities() -> Sequence['Ability']:

    def joystick(effect: 'Effect', message: 'Message.WhenEnemyActivateAgainstYou') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Give her 1 additional boost card for this activation and draw 1 card.",
                lambda targets:
                    message.GiveAdditionalBoostCardForThisActivation(1, effect)
            ),
            AbilityFactory.ForChoiceAbility(
                "Give her a tough status card.",
                lambda targets:
                    Faces.GiveStatus(targets, "Tough", effect)
            ).SetTarget([this], canbe_tough=True)
        )

    return [
        AbilityFactory.WhenEnemyActivateAgainstYou(
            AbilityType.ForcedInterrupt,
            "This",
            joystick
        ),
    ]

