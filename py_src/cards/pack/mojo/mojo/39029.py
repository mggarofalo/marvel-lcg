from . import *

# Supporting Actor

def GetAbilities() -> Sequence['Ability']:

    def supporting_actor(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        Faces.PlaceTokensOn([this], 2, 'threat', effect)

    def supporting_actor_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        faces = player.GetControlCharacters()
        Faces.PlaceTokensOn(faces, 1, 'threat', effect)


    return [
        AbilityFactory.AfterEnemyActivationEnd(
            AbilityType.ForcedResponse,
            "This",
            supporting_actor
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            supporting_actor_boost
        ),
    ]

