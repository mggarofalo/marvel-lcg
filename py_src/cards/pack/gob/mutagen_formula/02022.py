from . import *

# * Goblin Knight

def GetAbilities() -> Sequence['Ability']:

    def goblin_knight(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        faces = Worlds.DiscardEncounterCards(1, effect)
        for face in faces:
            if face and face.HasTrait("GOBLIN") and Minion.IsType(face):
                face.PutIntoPlay(player, effect)


    return [
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.ForcedResponse,
            "This",
            "You",
            goblin_knight
        ),
        AbilityFactory.WhenBoostShuffleThisIntoEncounterDeckAfterActivationEnd()
    ]

