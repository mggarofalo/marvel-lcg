from . import *

# No Place for the Living

def GetAbilities() -> Sequence['Ability']:

    def no_place_for_the_living_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action(player: 'Player'):
            damage = len(player.GetControlCardsByType(upgrade=True, support=True))
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Discard the upgrade or support they control with the highest cost",
                    lambda targets:
                        Faces.DiscardAll(targets, effect)
                ).SetTarget(CardFinder(card_type=Upgrade|Support), highest_cost=True, from_where=["YouControlCards"], canbe_discard=True),
                AbilityFactory.ForChoiceAbility(
                    f"Take {damage} damage",
                    lambda targets:
                        player.GetIdentity().TakeDamage(this, damage, effect)
                ).SetTarget("YourIdentity")
            )
        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            no_place_for_the_living_revealed
        ),
    ]

