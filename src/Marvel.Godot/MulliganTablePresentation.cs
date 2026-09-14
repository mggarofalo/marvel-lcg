using Marvel.Rules.Prompts;

namespace Marvel.Godot;

/// <summary>Prepares the table-owned controls for the current opening-hand prompt.</summary>
internal static class MulliganTablePresentation
{
    internal static BoardRenderResult Render(Main main, Prompt prompt, int expandedSeat, Action<int> switchSeat)
    {
        BoardRenderCleanup.Clear(main.boardAreas);
        BoardRenderCleanup.Clear(main.handRail);
        int cards = main.boardPresentation!.Areas.FirstOrDefault(area =>
            area.Zone == "HandsArea" && area.Seat == prompt.Player)?.Cards.Sum(card => card.Count) ?? 0;
        main.handHeading.Text = $"PLAYER {prompt.Player + 1} OPENING HAND  ·  {cards}  ·  SELECT REPLACEMENTS";
        var result = new BoardRenderResult();
        MulliganTableRenderer.Render(main.boardAreas, new MulliganTableContext
        {
            Board = main.boardPresentation,
            Hand = main.handRail,
            Result = result,
            Scale = main.interfaceScale,
            Art = main.art,
            Player = expandedSeat,
            PromptOwner = prompt.Player,
            SwitchSeat = switchSeat,
        });
        return result;
    }
}
