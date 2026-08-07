from . import *

# * Warbringer

def GetAbilities() -> Sequence['Ability']:

    def warbringer(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.property.against_player
        if player:
            damage = player.hand_cards.FindCardSize(CardFinder(has_printed_res="G"))
            this.GainForThisActive(effect, message, attack=damage)
            message.GainOverKill(effect)


    return [
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            "This",
            warbringer,
        ),
    ]

