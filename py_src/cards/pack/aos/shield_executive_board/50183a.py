from . import *

# Chief Tactical Officer

def GetAbilities() -> Sequence['Ability']:

    def chief_tactical_officer(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        player = message.GetToPlayer()

        if Faces.RemoveCountersOn([this], 1, 'secret', effect):
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Deal 2 damage to an enemy",
                    lambda targets:
                        this.DealDamage(targets, 2, effect)
                ).SetTarget(Enemy)
            )

    return [
        BoardMember(),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            chief_tactical_officer
        ).SetCost(Cost("RR"))
        .SetTarget("This", has_counter='secret'),
    ]

