from . import *

# * Samurai

def GetAbilities() -> Sequence['Ability']:

    def samurai(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'charge', effect)

        player = message.GetAgainstPlayer()
        if player:
            identity = player.GetIdentity()
            value = this.GetCounters('charge')
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Take damage equal to the number of charge counters on Samurai",
                    lambda targets:
                        identity.TakeDamage(this, value, effect)
                ).SetTarget("YourIdentity"),
                AbilityFactory.ForChoiceAbility(
                    "Discard 1 card you control with printed cost equal to or greater than the number of charge counters on Samurai",
                    lambda targets:
                        Faces.DiscardAll(targets, effect)
                ).SetTarget(CardFinder(cost_equal_or_greater=value), from_where=["YouControlCards"], canbe_discard=True)
            )


    return [
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.ForcedResponse,
            "This",
            samurai,
            against_who="You"
        ),
    ]

