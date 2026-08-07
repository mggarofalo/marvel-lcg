from . import *

# Curtain Call

def GetAbilities() -> Sequence['Ability']:

    def curtain_call_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        faces = Worlds.GetOnFieldCharacters(effect)
        faces = CardFinder(has_token='threat').Checks(faces)
        face = Filter.One(Math.AllMax(faces, lambda face: face.GetTokens('threat')), effect)
        scheme = Worlds.FindMainScheme(effect)
        do_move = False
        if face and scheme:
            num = Faces.RemoveTokensOn([face], "All", 'threat', effect)
            if num:
                do_move = this.PlaceThreatOnSchemes([scheme], num, effect)
        if not do_move:
            player = message.GetToPlayer()
            faces = player.GetControlCharacters()
            Faces.PlaceTokensOn(faces, 1, 'threat', effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            curtain_call_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            RevealThisCard
        ),
    ]

