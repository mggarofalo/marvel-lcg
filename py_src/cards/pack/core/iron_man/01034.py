from . import *

# * Stark Tower

def GetAbilities() -> Sequence['Ability']:

    def stark_tower(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        def action(player: 'Player'):
            found_card = None
            found_card = player.discard_pile.FindTopmost(CardFinder2("TECH", Upgrade))

            if found_card:
                Faces.ReturnToHand([found_card], player, effect)

        Players.ForEachTargets(effect.targets, action)


    def select_has_tech_upgrade_in_discard_pile_player(effect: 'Effect') -> Sequence['AlterEgo|Hero']:
        this = effect.this.CastTo(Support)
        Unused(this)

        units: List['AlterEgo|Hero'] = []
        for player in Worlds.GetPlayers(effect):
            for card in player.discard_pile.Get(True):
                if card.HasTrait("TECH") and Upgrade.IsType(card):
                    units.append(player.GetIdentity())
                    break
        return units

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            stark_tower
        ).SetTarget(select_has_tech_upgrade_in_discard_pile_player)
        .SetCostFunc(CostFunc.Exhaust("This"))
    ]

