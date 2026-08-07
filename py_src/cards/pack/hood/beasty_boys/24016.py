from . import *

# * Mandrill

def GetAbilities() -> Sequence['Ability']:

    def mandrill_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        Faces.GiveStatus(player.GetControlCharacters(), "Confused", effect)

    return [
        AbilityFactory.ThisGainKeywordForEachFaceInPlay(
            CardFinder(is_confused=True, card_type=Unit2),
            retaliate=1,
            change_on_event=OnEvent.Status(Unit2, 'Confused')
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            mandrill_revealed
        ),
    ]

