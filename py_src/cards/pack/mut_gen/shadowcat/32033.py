from . import *

# * Kitty's Room

def GetAbilities() -> Sequence['Ability']:

    def kittys_room(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()

        if initiator.form.IsInMassForm("Solid"):
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.HealthUnits(targets, 2, effect)
                ).SetTarget(name="Kitty Pryde", canbe_heal=True)
            )
        elif initiator.form.IsInMassForm("Phased"):
            initiator.DrawUp(1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            kittys_room
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

