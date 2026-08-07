from . import *

# * Taskmaster

def GetAbilities() -> Sequence['Ability']:

    def taskmaster_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        value = len(player.GetControlUpgrade())
        villain = Worlds.FindVillain(effect)
        if villain:
            villain.GainForThisActive(effect, message.being_message, attack=value, scheme=value)


    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                len(effect.this.CastTo(Minion).GetEngagedPlayer().GetControlUpgrade()),
            scheme=1,
            attack=1,
            change_on_event=OnEvent.PlayAreaCard("YouControlCards")
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            taskmaster_boost
        ),
    ]

