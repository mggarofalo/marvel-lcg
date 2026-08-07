from . import *

# Banishment

def GetAbilities() -> Sequence['Ability']:

    def banishment(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        identity = player.GetIdentity()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def discard_element_gun_and_this(targets: Sequence['CardFace']) -> None:
            Faces.DiscardAll(targets, effect)
            Faces.DiscardAll([this], effect)

        def place_thread_and_discard_this(targets: Sequence['CardFace']) -> None:
            this.PlaceThreatOnSchemes(targets, 3, effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Peter Quill → remove Banishment from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Discard an Element Gun from play. Discard this obligation",
                discard_element_gun_and_this
            ).SetTarget(name="Element Gun", canbe_discard=True),
            AbilityFactory.ForChoiceAbility(
                "Place 3 threat on the main scheme. Discard this obligation",
                place_thread_and_discard_this,
                condition=len([x for x in Worlds.GetOnFieldCards(identity.card.game_area) if x.IsName("Element Gun")]) == 0,
            ).SetTarget(MainScheme, can_place_threat=True),
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            banishment
        )
    ]

