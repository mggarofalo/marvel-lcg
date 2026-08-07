from . import *

# Mystic Senses

def GetAbilities() -> Sequence['Ability']:

    def mystic_senses(effect: 'Effect', message: 'Message.AfterEffectResolved') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.AfterPlayerResolveAbility(
            AbilityType.HeroResponse,
            "You",
            CardFinder(name="Adam Warlock"),
            mystic_senses,
            ability_name="Battle Mage",
        )
    ]

