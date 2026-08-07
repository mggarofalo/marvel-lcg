from . import *

# This Card is Fire

def GetAbilities() -> Sequence['Ability']:

    def this_card_is_fire_damage(effect: 'Effect', message: 'Message.WhenPlayerTurnEnd') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.GetIdentity().TakeDamage(this, 1, effect)


    def this_card_is_fire(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        damage = initiator.GetHero().sustained
        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.WhenPlayerTurnEnd(
            AbilityType.ForcedResponse,
            "This",
            this_card_is_fire_damage,
        ).CanWorkOnlyInHand(),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            this_card_is_fire
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

