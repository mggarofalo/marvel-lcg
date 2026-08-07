from . import *

# Bitter Rival

def GetAbilities() -> Sequence['Ability']:

    def bitter_rival_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        value = len(Worlds.GetSideSchemes(effect))
        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    Faces.ExhaustAll(targets, effect)
            ).SetTarget("YouControlUnit", canbe_exhaust=True,
                range=("Zero", value))
        )

    def bitter_rival_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    Faces.ExhaustAll(targets, effect)
            ).SetTarget("YouControlUnit", canbe_exhaust=True)
        )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            bitter_rival_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            bitter_rival_boost
        ),
    ]

