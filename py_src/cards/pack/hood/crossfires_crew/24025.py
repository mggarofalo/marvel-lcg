from . import *

# * Corruptor

def GetAbilities() -> Sequence['Ability']:

    def corruptor_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        faces = Faces.ExhaustAll(player.GetControlAllies(), effect)
        value = len(faces)
        if value > 0:
            scheme = Worlds.FindMainScheme(this)
            if scheme:
                this.PlaceThreatOnSchemes([scheme], value, effect)


    def corruptor_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        faces = player.GetControlCharacters(CardFinder(canbe_exhaust=True))
        face = player.AskChooseFace(faces, effect)
        if face:
            Faces.ExhaustAll([face], effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            corruptor_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            corruptor_boost
        ),
    ]

