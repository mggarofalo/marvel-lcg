from . import *

# Avalanche 9.0

def GetAbilities() -> Sequence['Ability']:

    def avalanche_90(effect: 'Effect', message: 'Message.AfterMinionEngagePlayer') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.engaged_player
        faces = player.GetControlCharacters(CardFinder(canbe_exhaust=True))
        face = player.AskChooseFace(faces, effect)
        if face:
            Faces.ExhaustAll([face], effect)
            this.DealDamage([face], 1, effect)

    def avalanche_90_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        faces = player.GetControlCharacters(CardFinder(canbe_exhaust=True))
        face = player.AskChooseFace(faces, effect)
        if face:
            Faces.ExhaustAll([face], effect)

    return [
        AbilityFactory.AfterMinionEngagePlayer(
            AbilityType.ForcedResponse,
            "This",
            "You",
            avalanche_90
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            avalanche_90_boost
        ),
    ]

