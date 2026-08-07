from . import *

# Hidden in Shadow

def GetAbilities() -> Sequence['Ability']:

    def hidden_in_shadow_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            if player == Worlds.GetFirstPlayer(effect) and Worlds.IsExpert(effect):
                damage = 2
            else:
                damage = 1

            this.DealIndirectDamage(player.GetIdentity(), damage, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            hidden_in_shadow_boost
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Enemy,
            hazard=1,
        ),
    ]

