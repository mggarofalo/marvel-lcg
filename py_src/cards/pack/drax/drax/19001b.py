from . import *

# * Drax

def GetAbilities() -> Sequence['Ability']:

    def drax(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        value = 2 * this.GetCounters('vengeance')
        Faces.RemoveCountersOn([this], "All", 'vengeance', effect)
        this.HealHealth(value, effect)


    return [
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.ForcedResponse,
            "You",
            drax,
            to_this_form=True,
            conditions=[
                lambda effect, message:
                    effect.this.CastTo(CanPlaceCounter).GetCounters('vengeance') > 0
            ],
        )
    ]

