from core import *
from engine.log import Log
from game.scene.replay import *
from game.world import *
from engine.config import ConfigVariables
from engine.controller import *

CATEGORY_NAME = "REPLAY"
DISABLE_DIGEST_ERROR_ASSERT = ConfigVariables.Bool('disable_digest_error_assert', False)
DIGEST_IGNORE_IDS           = ConfigVariables.ListInt('digest_ignore_ids', [])

def IsIgnorableMismatch(diff_ids: Sequence[int], ignore: Sequence[int]) -> bool:
    """Whether every card id that differs is one the run was told to ignore.

    A digest mismatch is tolerable only if nothing outside `digest_ignore_ids`
    moved. Anything else is a divergence and the replay must stop.

    MARVEL-43 fixed this under the v1 digest, and the reasoning carries over
    unchanged. The original read `all(x for x in diff_ids if x in ignore)`,
    which filters to the ignorable ids and then tests *those* for truthiness --
    three different wrong answers:

    - the ignore list defaults to `[]`, so the filtered sequence was empty and
      `all(<empty>)` was `True`. **Every mismatch was accepted.**
    - with a populated list, one ignorable id differing made the whole mismatch
      pass, however many non-ignorable ids differed alongside it.
    - card id 0 is falsy, so an ignorable id 0 was rejected rather than ignored.

    The v2 digest adds a fourth case the v1 shape could not produce. Its
    comparison is byte equality over a whole document, so the two strings can
    differ while no card record does -- the difference is then in the envelope,
    and no card id can explain it. That is a divergence, not an ignorable one.

    The ignore list is a parameter rather than a module global so the rule can be
    exercised without standing up a replay; the caller reads the config.
    """
    if not diff_ids:
        return False
    return all(object_id in ignore for object_id in diff_ids)

class InputModule:

    def __init__(self, manager: 'ControllerManager') -> None:
        self.replay_inputs: List['OperationDescriptor'] = []
        self.history_inputs: List['OperationDescriptor'] = []

        self.current_step_id = 0
        self.replay_step_id = 0

        self.is_updated = False

        self.calculated_digest: str = ""

        self.is_replay: bool = False

        self.break_on: List[int] = []

        self.manager = manager

    def Clean(self):
        self.replay_inputs = []
        self.history_inputs = []

        self.current_step_id = 0
        self.replay_step_id = 0
        self.is_updated = False
        self.calculated_digest = ""

    def Clear(self):
        self.break_on = []

    def SetReplayInputs(self, inputs: List['OperationDescriptor']):
        self.replay_inputs = inputs

    def SetIsReplay(self, replay: bool):
        self.is_replay = replay

    def SetBreakOn(self, break_on: List[int]):
        self.break_on = break_on

    def PrintStepID(self) -> str:
        from colorama import Fore, Style
        # replay_inputs_max_size = self.GetReplayOperationLen()
        # return f'{Fore.RED}#{Style.RESET_ALL}{self.current_step_id} ({controller_manager.last_turn_start_step_id} | {controller_manager.undo.last_step}) /{replay_inputs_max_size if replay_inputs_max_size > 0 else ""}'
        return f'{Fore.RED}(#{self.current_step_id} / {len(self.replay_inputs)}){Style.RESET_ALL}'

    ################################################################################
    #
    def Push(self, operation: 'OperationDescriptor'):
        self.history_inputs.append(operation)
        self.current_step_id += 1
        self.replay_step_id += 1

        if self.current_step_id in self.break_on:
            self.manager.skip.SetIsSkipping(False)

    def Pop(self):
        self.history_inputs.pop()
        self.current_step_id -= 1
        self.replay_step_id -= 1

    def GetReplayOperation(self, is_puzzle: bool, *, check_crc: bool=True) -> Tuple['OperationDescriptor|None', bool]:
        from engine import Engine
        self.is_updated = False

        if self.GetReplayOperationLen() <= self.replay_step_id:
            return None, True

        replay_input = self.replay_inputs[self.replay_step_id]
        if is_puzzle or not check_crc:
            return replay_input, True
        if Engine.game.controller_manager.console.debug_cmds:
            return replay_input, True

        recorded = replay_input.digest
        if not recorded:
            # A scene saved before `Versions.digest_v2` carried the v1 sum under
            # a different key, which `Json.ConvertDictToDataclass` drops. There
            # is nothing comparable, so the step replays on its inputs alone.
            Log.Warn(CATEGORY_NAME, self.MissingDigestReason())
            return replay_input, True
        if recorded == self.calculated_digest:
            return replay_input, True

        return replay_input, self.OnDigestMismatch(recorded)

    def MissingDigestReason(self) -> str:
        """Why this step has nothing to compare. Never raises -- it only explains."""
        from engine.lib.version import Ver, Versions
        version = self.manager.game.scene.version
        try:
            predates = bool(version) and Ver(version) < Versions.digest_v2
        except Exception:
            # `packaging.version.Version` rejects anything it does not recognise,
            # and the string came out of a file.
            return f"No digest recorded for this step (unreadable scene version {version!r})"
        if predates:
            return f"No comparable digest: scene version {version} predates the v2 digest"
        return "No digest recorded for this step"

    def OnDigestMismatch(self, recorded: str) -> bool:
        """Report a divergence and decide whether the replay may continue.

        The report is the point of the v2 format: it names the card, the zone
        and the field. v1 could only print a net delta per card id, so a change
        that added *n* to one field and subtracted *n* from another produced no
        row at all.
        """
        from engine import Engine
        from game.test import Test
        from game.world import digest

        disable_assert = DISABLE_DIGEST_ERROR_ASSERT.value

        try:
            diff_ids, report = digest.Diff(recorded, self.calculated_digest)
        except ValueError as exc:
            # Unreadable, so no ids to weigh against the ignore list. That lands
            # on `IsIgnorableMismatch`'s empty-`diff_ids` rule, which rejects --
            # an ignore list is a statement about cards and cannot excuse a
            # recording nothing can be read out of.
            diff_ids, report = [], f"unreadable digest: {exc}"

        if not disable_assert:
            header = f"Digest mismatch (#{self.current_step_id} / {len(self.replay_inputs)})\n"
            Log.Assert(CATEGORY_NAME, header + report)

        if Engine.in_unit_test:
            Engine.SaveCrash()
        if Test.IsInTesting():
            if not disable_assert:
                from core.lib.beep import Beep
                Beep.Warning()
                return False

        return IsIgnorableMismatch(diff_ids, DIGEST_IGNORE_IDS.value)

    def GetReplayOperationLen(self) -> int:
        return len(self.replay_inputs)

    def UpdateReplayStepId(self, diff: int):
        self.replay_step_id += diff
        self.is_updated = True

