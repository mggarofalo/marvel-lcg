from . import *

# Chief Medical Officer

def GetAbilities() -> Sequence['Ability']:

    def chief_medical_officer(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        player = message.GetToPlayer()

        if Faces.RemoveCountersOn([this], 1, 'secret', effect):
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Heal 1 damage from a friendly character",
                    lambda targets:
                        this.HealthUnits(targets, 1, effect)
                ).SetTarget(Friend, canbe_heal=True)
            )


    return [
        BoardMember(),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            chief_medical_officer
        ).SetCost(Cost("YY"))
        .SetTarget("This", has_counter='secret'),
    ]

