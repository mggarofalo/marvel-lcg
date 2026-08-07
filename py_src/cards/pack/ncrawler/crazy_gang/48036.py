from . import *

# * Executioner

def GetAbilities() -> Sequence['Ability']:

    def executioner_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        faces = Worlds.GetOnFieldFriendlyCharacters(effect)
        face = Filter.One(faces, effect, fewest_remaining_hp=True)
        if face:

            def action(face: CardFace):
                if Ally.IsType(face):
                    Faces.RemoveAllFromGame([face], effect)

            this.BasicAttack([face], effect, if_this_defeat_unit=action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            executioner_revealed
        ),
    ]

