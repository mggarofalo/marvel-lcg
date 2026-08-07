from . import *

# Volunteer Work

def GetAbilities() -> Sequence['Ability']:

    def volunteer_work(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()
        alter_ego = player.GetAlterEgo()
        this.RemoveThreatFromSchemes([this], alter_ego.recover, effect)
        if alter_ego.HasTrait("CIVILIAN"):
            player.DrawUp(1, effect)


    return [
        AbilityFactory.PlayersCannotThwartWhile(
            "You",
            "This"
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            volunteer_work
        ).SetCost(Cost("2")),
    ]

