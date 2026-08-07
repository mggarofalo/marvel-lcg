from . import *

# Split Personality

def GetAbilities() -> Sequence['Ability']:

    def split_personality(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.GetIdentity().ChangeToOtherIdentityForm(effect)
        size = initiator.GetIdentity().hand_size - initiator.hand_cards.GetSize()
        if size > 0:
            initiator.DrawUp(size, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            split_personality
        ).SetPlay().SetLabel(),
    ]

