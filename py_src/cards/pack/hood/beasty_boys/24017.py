from . import *

# Double Trouble

def GetAbilities() -> Sequence['Ability']:

    def double_trouble_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        faces = CardFinder(canbe_stunned=True).Checks(player.GetControlCharacters())
        unit = player.AskChooseFace(faces, effect)
        if unit:
            Faces.GiveStatus([unit], "Stunned", effect)
        faces = CardFinder(canbe_confused=True).Checks(player.GetControlCharacters())
        unit = player.AskChooseFace(faces, effect)
        if unit:
            Faces.GiveStatus([unit], "Confused", effect)

    def double_trouble_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        Faces.ResolveAbility([this], player, AbilityType.WhenRevealed, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            double_trouble_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            double_trouble_boost
        ),
    ]

