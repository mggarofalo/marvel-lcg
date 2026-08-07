from . import *

# Watch Me Play

def GetAbilities() -> Sequence['Ability']:

    def watch_me_play(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Obligation)
        player = this.GetGaveToPlayer()

        identity = player.GetIdentity()
        Faces.GiveStatus([identity], "Confused", effect)

    def watch_me_play_discard(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Obligation)
        player = this.GetGaveToPlayer()
        face = effect.cost_func.Get(CostFunc.Exhaust).return_exhausted_cards[0]
        player.AskDiscardFace(face.components.status.GetDeck().FindCards(name="Confused", card_type=StatusCard), effect)

    return [
        # TODO: When you look up a rule, you are confused
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            watch_me_play
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
            ex_operation=watch_me_play_discard
        ).SetCostFunc(CostFunc.Exhaust("YourIdentity", has_confuse_status=True))
        # .SetCostFunc(CostFunc.Discard(Select(lambda effect: effect.cost_target_list[0][0].components.status.GetDeck().FindCards(name="Confused", card_type=StatusFace))))
    ]

