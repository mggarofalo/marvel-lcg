from cards.pack import *

MODOK_FINDER = CardFinder(name="M.O.D.O.K.")

def GetHoldingCellDeck(effect: 'Effect'):
    return Worlds.ScenarioDeck(effect, "HoldingCell")

def HoldingCell(cost: 'Cost') -> List['Ability']:

    def holding_cell(effect: 'Effect', message: 'Message.AfterCardRemovedCounter') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        this.card.Flip(effect)
        face = this.card.face
        player = Worlds.FindPlayer(None, effect)
        if player:
            face.PutIntoPlay(player, effect, under_control=True)

        face = GetHoldingCellDeck(effect).GetTopCard()
        if face:
            face.PutIntoPlay("FirstPlayer", effect)

    def holding_cell_action(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        Faces.RemoveCountersOn([this], 1, 'lock', effect)


    return [
        AbilityFactory.ThisEnterPlayWithCounters(
            "2*", 'lock',
        ),
        AbilityFactory.WhenTheLastCounterIsRemovedFromHere(
            AbilityType.ForcedInterrupt,
            'lock',
            holding_cell
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            holding_cell_action
        ).SetCost(Cost("3", or_cost=cost)),
    ]

def FlyingInhumanLeavesPlay() -> 'Ability':

    def flying_inhuman_leaves(effect: 'Effect', message: 'Message.AfterCardLeavePlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 1, effect)
        this.card.Flip(effect)
        this.card.MoveToBottom(GetHoldingCellDeck(effect).deck, effect)

    return AbilityFactory.AfterCardLeavePlay(
            AbilityType.ForcedResponse,
            "This",
            flying_inhuman_leaves
        )

def AfterMODOKHitPointsAreResetDiscardThisCard():

    def automated_mobile_unit(effect: 'Effect', message: 'Message.AfterUnitHitPointReset') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.DiscardAll([this], effect)

    return AbilityFactory.AfterUnitHitPointReset(
        AbilityType.ForcedResponse,
        CardFinder(name="M.O.D.O.K."),
        automated_mobile_unit
    )

