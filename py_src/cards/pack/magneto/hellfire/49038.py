from . import *

# * Sebastian Shaw

def GetAbilities() -> Sequence['Ability']:

    def sebastian_shaw(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        Faces.GiveFacedownBoostCards([this], 1, effect)

        effects = this.effect.RegisterTemp(
            AbilityFactory.UnitCannotBeAttacked(
                "This"
            ),
            unregister_after_exec=False
        )

        def action():
            Effects.UnRegister(effects)

        RunAt.TheEndOfThePhase(effect, action)

    return [
        AbilityFactory.AfterUnitBeAttacked(
            AbilityType.ForcedResponse,
            "This",
            sebastian_shaw
        ),
    ]

