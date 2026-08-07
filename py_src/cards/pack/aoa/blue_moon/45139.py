from . import *

# Blue Area of the Moon

def GetAbilities() -> Sequence['Ability']:

    def blue_area_of_the_moon(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        player = message.GetToPlayer()
        if player:
            identity = player.GetIdentity()

            this.DealDamage([identity], 1, effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Minion,
            guard=1,
        ),
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            blue_area_of_the_moon
        ),
        WhenRevealedDiscardOtherSettingEnviroment()
    ]

