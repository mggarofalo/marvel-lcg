from cards.pack import *

def  IronManGetsSCHForEachTechAttachmentOnHim() -> 'Ability':
    return AbilityFactory.ThisGainKeyword(
        lambda effect, ui:
            min(3, len(effect.this.CastTo(Leader).GetAttachedAttachments())),
        scheme=1,
        change_on_event=OnEvent.AttachedMove(Attachment)
    )

