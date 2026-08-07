from . import *

# Badoon Assassin

def GetAbilities() -> Sequence['Ability']:

    def badoon_assassin(effect: 'Effect', message: 'Message.AfterMinionEngagePlayer') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.engaged_player
        this.DoAttackYou(player, effect, property=AttackProperty(additional_value=2))

    def badoon_assassin_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        atk_message = message.would_atk_message
        atk_message.GainOverKill(effect)
        atk_message.GainPiercing(effect)
        atk_message.GainRanged(effect)


    return [
        AbilityFactory.AfterMinionEngagePlayer(
            AbilityType.ForcedResponse,
            "This",
            "YouHero",
            badoon_assassin
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            badoon_assassin_boost,
            during_attack=True,
        ),
    ]

