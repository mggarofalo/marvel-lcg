from core import *
from enum import Enum

from game.player import *

class Phase:
    class State(str, Enum):
        Initialize          = "Initialize"
        ScenarioSetup       = "Scenario Setup"
        ResolveMulligans    = "Resolve Mulligans"
        InitFinished        = "Init Finished"
        PlayerTurn          = "Player Turn"
        PlayerTurnEnd       = "Player Turn End"
        PlaceThreat         = "Main Scheme Place Threat"
        EnemyActivation     = "Enemy Activation"
        DealEncounterCards  = "Deal Encounter Cards"
        RevealsEncounterCards  = "Reveal Encounter Cards"
        EndPhase            = "End Phase"
        EndRound            = "End Round"
        StartRound          = "Start Round"

    def __init__(self) -> None:
        self.state = Phase.State.Initialize

    def SetState(self, state: 'State'):
        self.state = state

    def IsVillainPhase(self) -> bool:
        return self.state in [
            Phase.State.PlaceThreat,
            Phase.State.EnemyActivation,
            Phase.State.DealEncounterCards,
            Phase.State.RevealsEncounterCards,
        ]

    def IsVillainPhaseStep(self, step: int) -> bool:
        steps: Dict[int, Phase.State] = {
            1: Phase.State.PlaceThreat,
            2: Phase.State.EnemyActivation,
            3: Phase.State.DealEncounterCards,
            4: Phase.State.RevealsEncounterCards,
        }
        state = steps[step]
        return self.state == state

    def IsPlayerPhase(self, player: 'Player|Literal["AnyPlayer"]') -> bool:
        if self.state != Phase.State.PlayerTurn:
            return False
        if player != "AnyPlayer":
            return player.world.GetCurrentPlayer() == player
        return True

    def IsGameStarted(self) -> bool:
        return self.state != Phase.State.Initialize and \
            self.state != Phase.State.ScenarioSetup

    def IsGameInitializing(self) -> bool:
        return self.state == Phase.State.Initialize

    def HasCurrentPlayerPhase(self) -> bool:
        return self.state in [
            Phase.State.PlayerTurn,
            Phase.State.PlayerTurnEnd,
            Phase.State.EnemyActivation,
            Phase.State.DealEncounterCards
        ]

PhaseName = Literal["Player", "Villain"]

