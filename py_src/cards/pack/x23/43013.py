from . import *

# * Boom Boom: Tabitha Smith

def GetAbilities() -> Sequence['Ability']:

    def boom_boom(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.PlaceCountersOn([this], 1, 'boom', effect)

        def action(targets: Sequence[CardFace]):
            Faces.DiscardAll([this], effect)
            damage = this.GetCounters('boom')
            this.DealDamage(targets, damage, effect)

        initiator.MayChooseOneAbility(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Discard Boom Boom to deal 1 damage to each enemy for each boom counter on her",
                action
            ).SetTarget(Enemy, range="All")
        )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            boom_boom
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

