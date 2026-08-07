from . import *

# Pursued by the Past

def GetAbilities() -> Sequence['Ability']:

    def pursued_by_the_past(effect: 'Effect', message: 'Message.AfterCardPlacedCounter') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        player = message.place_player

        if player:
            minion = Worlds.FindCardOnField(
                effect,
                card_type=Minion,
                is_nemesis=player,
            )
            if minion:
                minion.DoActivate(player, effect)
            else:
                this.card.Flip(effect)

    return [
        AbilityFactory.AfterCounterPlacedOn(
            AbilityType.ForcedResponse,
            "This",
            'pursuit',
            pursued_by_the_past,
            at_least=lambda effect: 3 + Worlds.GetPlayerNum(effect)
        ).SetCostFunc(CostFunc.RemoveEachCounter("This", 'pursuit')),
    ]

