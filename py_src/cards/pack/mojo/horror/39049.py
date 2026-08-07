from . import *

# Cultist

def GetAbilities() -> Sequence['Ability']:

    def cultist(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()

        face = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            name="The Kraken",
            card_type=Minion,
        )

        if face:
            face.PutIntoPlay(player, effect)
            Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            "This",
            cultist,
        ),
    ]

