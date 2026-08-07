from . import *

# Athletic Conditioning

def GetAbilities() -> Sequence['Ability']:

    def athletic_conditioning(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.DiscardAll(effect.targets, effect)

    def your_stunned_and_confused(effect: 'Effect') -> Sequence['StatusCard']:
        status_face: List[StatusCard] = []
        hero = effect.GetInitiator().GetHero()
        for status in hero.components.status.GetDeck().Get():
            if status.name == "Confused":
                status_face.append(status)
            if status.name == "Stunned":
                status_face.append(status)
        return status_face

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            athletic_conditioning,
        ).SetPlay().SetLabel()
        .SetTarget(your_stunned_and_confused)
    ]

