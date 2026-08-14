from . import *

# Vibranium Suit

def GetAbilities() -> Sequence['Ability']:

    def vibranium_suit(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()
        target = effect.targets[0].CastTo(Unit2)

        damage = 1
        if effect.this == message.sequence[-1]:
            damage = 2
        # No clamp here: `MoveDamage` moves no more than the hero is carrying,
        # and it reads `GetLostHealth()` on this same hero with nothing in
        # between. A private copy of that rule made the engine's own clamp
        # unobservable to this card's scenarios. See MARVEL-122.
        hero.MoveDamage(damage, target, effect)

    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            vibranium_suit,
        ).SetLabel('attack')
        .SetTarget(Enemy)
        .SetTarget2("YourHero", sustained=True)
    ]

