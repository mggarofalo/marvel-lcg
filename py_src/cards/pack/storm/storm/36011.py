from . import *

# Lightning Bolt

def GetAbilities() -> Sequence['Ability']:

    def lightning_bolt(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 8, effect)

        initiator = effect.GetInitiator()
        thunderstorm = Worlds.FindCardOnField(
            effect,
            name="Thunderstorm",
            card_type=Support
        )
        if thunderstorm:
            thunderstorm.ResolveSpecialAbility(initiator, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            lightning_bolt
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

