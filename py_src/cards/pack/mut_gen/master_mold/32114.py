from . import *

# Sentinel Mark VIII

def GetAbilities() -> Sequence['Ability']:

    def sentinel_mark_viii(effect: 'Effect', message: 'Message.AfterMinionEngagePlayer') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        face = Worlds.GetEncounterDiscardPile(effect).FindTopmost(CardFinder2("SENTINEL", Attachment))
        if face:
            face.AttachTo2(this, effect)

    return [
        AbilityFactory.AfterMinionEngagePlayer(
            AbilityType.ForcedResponse,
            "This",
            "You",
            sentinel_mark_viii
        ),
    ]

