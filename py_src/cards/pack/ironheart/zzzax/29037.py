from . import *

# * Zzzax

def GetAbilities() -> Sequence['Ability']:

    def zzzax(effect: 'Effect', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = this.GetEngagedPlayer()
        return FacesCounter.GetPrintedResourcesCount(player.GetControlCards(), "Y")

    def zzzax_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        this.PutIntoPlay(player, effect)


    return [
        AbilityFactory.ThisGainKeyword(
            zzzax,
            attack=1,
            health=1,
            change_on_event=OnEvent.PlayAreaCard("YouControlCards"),
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            zzzax_boost,
            conditions=[
                lambda effect, message:
                    FacesCounter.GetPrintedResourcesCount(message.GetToPlayer().hand_cards.GetAll(), "Y") >= 2
            ]
        ),
    ]

