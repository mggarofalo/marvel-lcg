from . import *

# Unleash Nova Force

def GetAbilities() -> Sequence['Ability']:

    def unleash_nova_force(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        unit = effect.targets[0]
        initiator = effect.GetInitiator()

        def action():
            Faces.ReadyAll([unit], effect)
            initiator.DrawUp(1, effect)

        unit.effect.RegisterTemp(
            AbilityFactory.AfterUnitDefeatedUnit(
                AbilityType.Temp0,
                "This",
                Enemy,
                lambda effect, message:
                    action(),
            ),
            unregister_after_exec=False,
            until_round_end=True
        )

        unit.effect.RegisterTemp(
            AbilityFactory.AfterSchemeRemoveThreat(
                AbilityType.Temp0,
                None,
                lambda effect, message:
                    action(),
                last_threat=True,
                by_who="This"
            ),
            unregister_after_exec=False,
            until_round_end=True
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            unleash_nova_force
        ).SetPlay().SetLabel()
        .SetTarget(name="Nova")
        .MaxOnePerRound(),
    ]

