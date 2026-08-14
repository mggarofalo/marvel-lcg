from . import *

# Back in Action

def GetAbilities() -> Sequence['Ability']:

    def back_in_action_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        if villain:
            Faces.GiveStatus([villain], "Tough", effect)
        num = len(GetVillainsUnderRouted(effect))
        this.PlaceThreatOnSchemes("MainScheme", num, effect)

    def back_in_action_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        if player.IsControl(CardFinder(name="Morlock", card_type=Ally)):
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Deal 1 damage to it",
                    lambda targets:
                        this.DealDamage(targets, 1, effect)
                ).SetTarget("YourAlly", name="Morlock"),
                AbilityFactory.ForChoiceAbilityToSpend(
                    Cost("1")
                )
            )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            back_in_action_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            back_in_action_boost
        ),
    ]

