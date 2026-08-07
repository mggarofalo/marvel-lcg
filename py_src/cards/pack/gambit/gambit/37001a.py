from . import *

# * Gambit

def GetAbilities() -> Sequence['Ability']:

    def gambit(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        Faces.PlaceCountersOn(effect.targets, 1, 'charge', effect)

    def throw_de_card(effect: 'Effect', message: 'Message.WhenPlayerPlayCard') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        damage = effect.cost_func.Get(CostFunc.Counter).return_remove_counter

        this.effect.RegisterTemp(
            AbilityFactory.WhenFaceWouldDealDamage(
                AbilityType.Temp0,
                None,
                lambda effect, message:
                    message.IncreaseDamage(+damage, effect),
                by_play_card=message.played_face,
                allow_zero_damage=True
            ),
            unregister_after_exec=False,
            until_event_end=message
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            gambit
        ).SetName("Charge de Card")
        .SetTarget("This")
        .LimitOncePerRound(),
        AbilityFactory.WhenPlayerPlayCard(
            AbilityType.Interrupt,
            "You",
            CardFinder2("ATTACK", Event),
            throw_de_card
        ).SetName("Throw de Card")
        .SetTarget("This")
        .SetCostFunc(CostFunc.Counter("This", (1, 3), 'charge'))
    ]

