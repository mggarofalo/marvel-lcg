from . import *

# Hammer Throw

def GetAbilities() -> Sequence['Ability']:

    def hammer_throw(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        this.DealDamage(effect.targets, 8, effect, property=AttackProperty(overkill=True))
        faces = effect.cost_func.Get(CostFunc.Exhaust).return_exhausted_cards
        Faces.ReturnToHand(faces, initiator, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            hammer_throw
        ).SetPlay().SetLabel('attack')
        .SetCostFunc(CostFunc.Exhaust(
            name="Mjolnir",
            card_type=Upgrade,
            from_where=["YouControlCards"]))
        .SetTarget(Enemy),
    ]

