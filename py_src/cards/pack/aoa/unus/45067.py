from . import *

# Infinite Prelate

def GetAbilities() -> Sequence['Ability']:

    def infinite_prelate_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindCardOnField(effect, name="Unus", card_type=Enemy)

        if villain:
            def action(act_message: 'Message.WhenEnemyActivateAgainstYou') -> None:
                value = GetThreatOnGenePool(effect)
                if value >= 3:
                    Faces.GiveStatus([villain], "Tough", effect)
                if value >= 6:
                    this.HealthUnits([villain], 3, effect)
                if value >= 9:
                    act_message.GiveAdditionalBoostCardForThisActivation(1, effect)

            villain.DoActivate(player, effect, operation=action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            infinite_prelate_revealed
        ),
    ]

