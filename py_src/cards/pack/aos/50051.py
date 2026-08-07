from . import *

# Intelligence

def GetAbilities() -> Sequence['Ability']:

    def intelligence(effect: 'Effect', message: 'Message.AfterPlayerDealEncounterCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()

        faces = [x for player in Worlds.GetPlayers(effect) for x in player.dealt_encounter_cards.Get()]
        faces += Worlds.GetEncounterDeck(effect).GetTops(1)
        Faces.LookAt(faces, initiator, effect)

        initiator.SwapTheseCards(faces, effect)


    return [
        AbilityFactory.AfterPlayerDealEncounterCard(
            AbilityType.Response,
            intelligence
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

