from . import *

# Badoon Ship

def GetAbilities() -> Sequence['Ability']:

    def badoon_ship(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'barrage', effect)
        if this.GetCounters('barrage') >= 4:
            def action(player: 'Player'):
                this.DealIndirectDamage(player.GetIdentity(), 2, effect)

            Players.ForEachPlayer(effect, action)

            Faces.RemoveCountersOn([this], "All", 'barrage', effect)


    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            badoon_ship
        ).SetName("Charge Up")
    ]

