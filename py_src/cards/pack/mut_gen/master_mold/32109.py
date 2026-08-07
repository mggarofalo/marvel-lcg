from . import *

# * Master Mold

def GetAbilities() -> Sequence['Ability']:

    def master_mold(effect: 'Effect', message: 'Message.WhenUnitWouldScheme') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetAgainstPlayer()

        if player:
            minion = Worlds.DiscardEncounterCardsUntil(
                effect,
                card_type=Minion,
                trait="SENTINEL",
            )
            if minion:
                minion.PutIntoPlay(player, effect)

            message.DoNotGiveBoostCardForThisActivation(effect)

    return [
        AbilityFactory.WhenUnitSchemeAgainst(
            AbilityType.ForcedInterrupt,
            "This",
            "You",
            master_mold
        ),
    ]

