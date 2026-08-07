from . import *

# Gunslinger

def GetAbilities() -> Sequence['Ability']:

    def gunslinger(effect: 'Effect', message: 'Message.WhenMinionEngagePlayer') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.engaged_player
        if player.IsHero():
            if player.AskSpendResources(Cost("YY"), effect):
                hero = player.GetHero()
                hero.DealDamage([this], hero.attack, effect)

    return [
        AbilityFactory.WhenMinionEngagePlayer(
            AbilityType.ForcedInterrupt,
            "This",
            "You",
            gunslinger
        ),
    ]

