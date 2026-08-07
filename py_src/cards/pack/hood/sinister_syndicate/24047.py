from . import *

# * White Rabbit

def GetAbilities() -> Sequence['Ability']:

    def white_rabbit(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        for target in message.attacked_targets:
            player = target.GetControlBy()
            if Player.IsType(player):
                player.DiscardRandomHandCards(1, effect)


    def white_rabbit_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardHandCards((1, 1), effect, finder=CardFinder(card_class="IdentitySpecific"))


    return [
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            "This",
            white_rabbit,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            white_rabbit_boost
        ),
    ]

