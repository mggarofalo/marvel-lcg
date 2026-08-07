from . import *

# Hot-Headed

def GetAbilities() -> Sequence['Ability']:

    def hot_headed(effect: 'Effect', message: 'Message.AfterCardAttachTo') -> None:
        this = effect.this.CastTo(Obligation)

        player = this.GetBindFace().GetControlByPlayer()
        player.GetIdentity().TakeDamage(this, 1, effect)


    return [
        AbilityFactory.AfterCardGainUpgrade(
            AbilityType.ForcedResponse,
            CardFinder(name="Frostbite", card_type=Upgrade),
            Enemy,
            hot_headed
        ),
        AbilityFactory.AfterUnitMakeRecovery(
            AbilityType.AlterEgoResponse,
            "You",
            DiscardThisCard
        ),
    ]

