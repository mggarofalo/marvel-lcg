from . import *

# Charged Card

def GetAbilities() -> Sequence['Ability']:

    class Buff_37006(Buff):
        def __init__(self) -> None:
            super().__init__()
            self.counter = 0

        def Update(self, counter: int):
            self.counter = counter

    def charged_card(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        counter = effect.GetInitiator().GetIdentity().GetBuff(Buff_37006).counter

        ranged = False
        piercing = False
        overkill = False

        if counter:
            if counter >= 1:
                ranged = True
            if counter >= 2:
                piercing = True
            if counter >= 3:
                overkill = True

        this.DealDamage(effect.targets, 4, effect, property=AttackProperty(ranged=ranged, piercing=piercing, overkill=overkill))

    def charged_card_remove(effect: 'Effect', message: 'Message.AfterEffectResolved') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        assert isinstance(message.message, Message.WhenPlayerPlayCard)
        assert isinstance(message.message.played_effect.bind_message, Message.WhenPlayerInTurn)

        counter = message.effect.cost_func.Get(CostFunc.Counter).return_remove_counter

        message.effect.GetInitiator().GetIdentity().GetBuff(Buff_37006).Update(counter)

    return [
        AbilityFactory.AfterPlayerResolveAbility(
            AbilityType.Temp0,
            "You",
            None,
            charged_card_remove,
            ability_name="Throw de Card",
            conditions=[
                lambda effect, message:
                    effect.this.card.IsOnProcessingArea() and \
                    isinstance(message.message, Message.WhenPlayerPlayCard) and \
                    effect.this == message.message.played_effect.this,
            ]
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            charged_card,
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

