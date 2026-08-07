from . import *

# Thunderbolt Backup

def GetAbilities() -> Sequence['Ability']:

    def thunderbolt_backup(effect: 'Effect', message: 'Message.WhenRoundEnd') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        minion_most_damage = Filter.One(
            Worlds.GetOnFieldMinions(effect, CardFinder2("THUNDERBOLT")),
            effect,
            most_damage=True,
        )

        minions = this.GetInventoryDeck().FindCards(card_type=Minion)
        minion_here = Filter.One(minions, effect)

        if minion_here and minion_most_damage:
            player = minion_most_damage.engaged_player
            if player:
                minion_here.PutIntoPlay(player, effect)

        if minion_most_damage:
            minion_most_damage.AttachTo2(this, effect)

        minions = this.GetInventoryDeck().FindCards(card_type=Minion)

        if Worlds.IsExpert(effect):
            this.HealthUnits(minions, "2*", effect)
            Faces.GiveStatus(minions, "Tough", effect)
        else:
            this.HealthUnits(minions, "1*", effect)


    return [
        AbilityFactory.WhenRoundEnd(
            AbilityType.ForcedInterrupt,
            None,
            thunderbolt_backup
        ),
    ]

