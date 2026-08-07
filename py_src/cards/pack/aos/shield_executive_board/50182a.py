from . import *

# Chief Surveillance Officer

def GetAbilities() -> Sequence['Ability']:

    def chief_surveillance_officer(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        player = message.GetToPlayer()

        if Faces.RemoveCountersOn([this], 1, 'secret', effect):
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Remove 2 threat from a scheme",
                    lambda targets:
                        this.RemoveThreatFromSchemes(targets, 2, effect)
                ).SetTarget(Scheme2)
            )


    return [
        BoardMember(),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            chief_surveillance_officer
        ).SetCost(Cost("BB"))
        .SetTarget("This", has_counter='secret'),
    ]

