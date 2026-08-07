from . import *

# Zola's Experiments

def GetAbilities() -> Sequence['Ability']:

    def zolas_experiments(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        minion = message.trigger

        encounter_discard_pile = Worlds.GetEncounterDiscardPile(effect)
        face = encounter_discard_pile.FindTopmost(CardFinder2("TECH", Attachment))
        if face:
            face.AttachTo2(minion, effect)

    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.ForcedResponse,
            Minion,
            zolas_experiments
        )
    ]

