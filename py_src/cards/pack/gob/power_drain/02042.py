from . import *

# * Electro

def GetAbilities() -> Sequence['Ability']:

    def electro(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()

        if player:
            faces = Worlds.DiscardEncounterCards(1, effect)
            size = FacesCounter.CountTotalBoostIcons(faces)

            player.GetIdentity().TakeIndirectDamage(this, size, effect)


    def electro_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        Worlds.DiscardEncounterCards(3, effect)


    return [
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            "This",
            electro
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            electro_boost
        ),
    ]

