from . import *

# Power and Decadence

def GetAbilities() -> Sequence['Ability']:

    def power_and_decadence_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        if villain:
            Faces.GiveStatus([villain], "Tough", effect)
            villain.GiveBoostCard(this, effect)


    def power_and_decadence_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        def action():
            message.activating_enemy.DoActivate(player, effect, do_not_give_boost=True)

        message.AfterThisActivation(
            effect,
            action
        )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            power_and_decadence_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            power_and_decadence_boost
        ),
    ]

