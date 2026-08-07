from . import *

# * Daredevil

def GetAbilities() -> Sequence['Ability']:

    def daredevil_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        team = Worlds.GetEnemyTeam(effect)
        team.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    Faces.ExhaustAll(targets, effect)
            ).SetTarget(player.GetControlCharacters(), canbe_exhaust=True)
        )

    def daredevil_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    Faces.ExhaustAll(targets, effect)
            ).SetTarget(player.GetControlCharacters(), canbe_exhaust=True)
        )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            daredevil_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            daredevil_boost
        ),
    ]

