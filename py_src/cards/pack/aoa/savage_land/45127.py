from . import *

# The Savage Land

def GetAbilities() -> Sequence['Ability']:

    def the_savage_land(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        player = message.GetToPlayer()
        if player:
            player.DiscardDeckTopCards(3, effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Villain,
            retaliate=1,
        ),
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            the_savage_land
        ),
        WhenRevealedDiscardOtherSettingEnviroment()
    ]

