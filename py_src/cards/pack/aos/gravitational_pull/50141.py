from . import *

# Gravitational Pull

def GetAbilities() -> Sequence['Ability']:

    def gravitational_pull_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        minion = Find.FindAndReveal(
            effect,
            player,
            name="Moonstone",
            card_type=Enemy
        )

        def action():
            ThisCardGainSurge(effect)

        if minion:
            minion.DoActivate(player, effect, if_no_activate=action)
        else:
            action()

    def gravitational_pull_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust a character you control",
                lambda targets:
                    Faces.ExhaustAll(targets, effect)
            ).SetTarget("YouControlUnit", canbe_exhaust=True)
        )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            gravitational_pull_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            gravitational_pull_boost
        ),
    ]

