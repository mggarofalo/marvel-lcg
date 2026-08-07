from . import *

# Hide!

def GetAbilities() -> Sequence['Ability']:

    def hide_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    Faces.GiveStatus(targets, "Tough", effect)
            ).SetTarget(Ally, trait="MORLOCK", canbe_tough=True)
        )

    def hide_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    Faces.GiveStatus(targets, "Tough", effect)
            ).SetTarget(Ally, trait="MORLOCK", canbe_tough=True)
        )

        message.GiveBoostCardForThisActivation(Villain, 1, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            hide_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            hide_boost
        ),
    ]

