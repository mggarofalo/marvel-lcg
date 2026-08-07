from . import *

# Ultimate Bio-Servant

def GetAbilities() -> Sequence['Ability']:

    def ultimate_bio_servant(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        if villain:
            Faces.GiveStatus([villain], "Tough", effect)

    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                len(effect.this.CastTo(Minion).GetAttachedAttachments()),
            attack=1,
            change_on_event=OnEvent.AttachedMove(Attachment)
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            ultimate_bio_servant
        )
    ]

