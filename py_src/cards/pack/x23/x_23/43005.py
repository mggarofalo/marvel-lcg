from . import *

# Claw Mastery

def GetAbilities() -> Sequence['Ability']:

    def claw_mastery(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        for face in effect.targets:
            face.GainUntilRoundEnd(
                effect,
                attack=2
            )

            face.effect.RegisterTemp(
                AbilityFactory.UnitAttackGainKeyword(
                    "This",
                    overkill=True,
                    conditions=[
                        lambda effect, message:
                            Worlds.FindCardOnField(effect, name="Honey Badger") != None
                    ]
                ),
                unregister_after_exec=False,
                until_round_end=True
            )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            claw_mastery
        ).SetPlay().SetLabel()
        .SetTarget(name="X-23")
        .MaxOnePerRound(),
    ]

