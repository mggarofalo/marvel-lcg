from . import *

# * Nanny

def GetAbilities() -> Sequence['Ability']:

    def nanny(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()
        if player:
            face = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=True,
                include_set_aside=True,
                name='"Lost" Child'
            )
            if face:
                face.Reveal(player, effect)

    def control_ally(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> bool:
        player = message.GetAgainstPlayer()
        if player:
            return len(player.GetControlAllies()) >= 1
        return False

    return [
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            "This",
            nanny,
            conditions=[control_ally]
        ),
    ]

