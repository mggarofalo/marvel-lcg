from . import *

# City Streets

def GetAbilities() -> Sequence['Ability']:

    def city_streets(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        face = effect.cost_func.Get(CostFunc.Exhaust).return_exhausted_cards[0]
        if HasAttack.IsType(face):
            value = face.attack
            Faces.RemoveCountersOn([this], value, 'sand', effect)

    def surging_sands(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'sand', effect)
        sand = this.GetCounters('sand')
        Worlds.DiscardEncounterCards(sand, effect)


    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            surging_sands
        ).SetName(SURGING_SANDS),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            city_streets,
            conditions=[
                lambda effect, message:
                    effect.this.CastTo(CanPlaceCounter).GetCounters('sand') > 0,
            ]
        ).SetCostFunc(CostFunc.Exhaust("YouControlUnit"))
        .LimitOncePerRoundPerPlayer(),
    ]

