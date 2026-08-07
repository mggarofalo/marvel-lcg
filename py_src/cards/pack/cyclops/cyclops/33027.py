from . import *

# Lost Visor

def GetAbilities() -> Sequence['Ability']:

    def lost_visor(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        face = Search.SearchForCard(
            effect,
            player,
            include_player_hand_cards=True,
            include_player_deck=True,
            include_player_discard_pile=True,
            include_player_area=True,
            name="Ruby Quartz Visor",
        )
        if face:
            this.PlaceCardHere(face, False, effect)

    def lost_visor_action(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Obligation)

        initiator = effect.GetInitiator()
        initiator.GainCard(this.GetPlacedCardArea().Get(), effect)

        Faces.RemoveAllFromGame([this], effect)

    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            lost_visor
        ),
        *AbilityFactory.UnitCannotAttackTarget(
            CardFinder(name="Scott Summers"),
            cannot_attack=True,
            cannot_trigger_attack_ability=True
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            lost_visor_action,
        ).SetCostFunc(CostFunc.Exhaust(name="Scott Summers")),
    ]

