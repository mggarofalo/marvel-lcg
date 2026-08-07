from . import *

# * Apocryphus

def GetAbilities() -> Sequence['Ability']:

    def apocryphus_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardControlCards(effect, ally=True, support=True)

    def apocryphus_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
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
        message.GiveBoostCardForThisActivation(Enemy, 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            apocryphus_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            apocryphus_boost
        ),
    ]

