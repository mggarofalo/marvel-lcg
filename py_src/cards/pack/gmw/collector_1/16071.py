from . import *

# * Collector

def GetAbilities() -> Sequence['Ability']:

    def collector_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        def action(player: 'Player'):
            def put_card():
                PutTopCardIntoTheCollection(player, effect)

            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Put the top card of their deck faceup into The Collection",
                    lambda targets:
                        put_card()
                ),
                AbilityFactory.ForChoiceAbility(
                    "Take 3 damage",
                    lambda targets:
                        player.GetIdentity().TakeDamage(this, 3, effect)
                ).SetTarget("YourIdentity"),
            )

        Players.ForEachPlayer(effect, action)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            collector_revealed
        ),
        PutCardIntoTheCollectionInsteadDiscard(False)
    ]

