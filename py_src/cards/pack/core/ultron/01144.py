from . import *

# Android Efficiency

def GetAbilities() -> Sequence['Ability']:

    def android_efficiency_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action(player: 'Player'):
            CreateUltronFacedownDrone(player, 1, effect)

        Players.ForEachPlayer(effect, action)

    def android_efficiency_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        def create_drone(targets: Sequence['CardFace']) -> None:
            CreateUltronFacedownDrone(player, 1, effect)

        cost_dict: Dict[str, Literal["Y", "B", "R"]] = {
            "01144": "Y",
            "01144a": "Y",
            "01144b": "B",
            "01144c": "R",
        }
        cost = Cost(cost_dict[this.paper.card_id])

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbilityWithCost(
                cost,
            ),
            AbilityFactory.ForChoiceAbility(
                "Put the top card of the deck into play facedown, engaged with you as a DRONE minion",
                create_drone
            )
        )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            android_efficiency_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            android_efficiency_boost
        )
    ]
