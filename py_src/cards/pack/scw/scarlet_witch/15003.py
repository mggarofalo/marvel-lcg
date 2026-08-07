from . import *

# Chaos Magic

def GetAbilities() -> Sequence['Ability']:

    def chaos_magic(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.PlayCardsLikeInTurn(effect.targets, effect, ignore_resources_cost=True,
            after_play=lambda face:
                Worlds.DiscardEncounterCards(FacesCounter.GetPrintedResourceCost([face]), effect)
        )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            chaos_magic
        ).SetPlay().SetLabel()
        .SetTarget(canbe_play=True, from_where=["YourHandCards"])
    ]

