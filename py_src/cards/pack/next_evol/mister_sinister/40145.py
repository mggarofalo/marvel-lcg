from . import *

# Sinister Soldier

def GetAbilities() -> Sequence['Ability']:

    def sinister_soldier(effect: 'Effect', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Minion)
        Unused(this)

        villain = FindMisterSinister(effect)
        if villain:
            faces = villain.GetInventoryDeck().FindCards(CardFinder2("SUPERPOWER", Attachment))
            if faces:
                ui.extend(faces)
                return len(faces)
        return 0


    def sinister_soldier_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        villain = FindMisterSinister(effect)
        if villain:
            value = villain.GetInventoryDeck().FindCardSize(CardFinder2("SUPERPOWER", Attachment))
            villain.GainForThisActive(
                effect,
                message.would_message,
                attack=value,
                scheme=value
            )


    return [
        AbilityFactory.ThisGainKeyword(
            sinister_soldier,
            attack=1,
            scheme=1,
            change_on_event=OnEvent.AttachedMove(Attachment)
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            sinister_soldier_boost
        ),
    ]

