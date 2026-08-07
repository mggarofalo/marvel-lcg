from . import *

# Infinite Hunter

def GetAbilities() -> Sequence['Ability']:

    def infinite_hunter_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        allies = player.GetControlAllies()
        ally = player.AskChooseFace(allies, effect)
        if ally:
            this.DealDamage([ally], 3, effect)


    def infinite_hunter_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        enemy = message.activating_enemy

        def gain_sch_and_atk():
            enemy.GainForThisActive(effect, message.being_message, attack=2, scheme=2)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Place 2 threat on Gene Pool",
                lambda targets:
                    this.PlaceThreatOnSchemes(targets, 2, effect)
            ).SetTarget(name="Gene Pool"),
            AbilityFactory.ForChoiceAbility(
                "Activating enemy gets +2 SCH and +2 ATK for this activation",
                lambda faces:
                    gain_sch_and_atk()
            ).SetTarget([enemy]),
        )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            infinite_hunter_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            infinite_hunter_boost
        ),
    ]

