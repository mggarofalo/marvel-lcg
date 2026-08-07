from . import *

# Phase Disruption

def GetAbilities() -> Sequence['Ability']:

    def phase_disruption(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Confused", effect)

        initiator = effect.GetInitiator()
        Players.DiscardHeroActionAttachment(initiator, effect.targets, effect, may=False)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            phase_disruption
        ).SetPlay(only_if_you_are_in_additional_from="Intangible").SetLabel()
        .SetTarget(Enemy, check_fn=
                lambda effect, face:
                    Enemy.IsType(face) and ( \
                        face.CanbeConfused() or \
                        face.GetInventoryDeck().FindCard(with_texts=["Hero Action", "Hero Response"]) != None
                    )
            )
    ]

