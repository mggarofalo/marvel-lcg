using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Encounter schemes and environments expose a full-width inspectable header for every copy.</summary>
internal static class SchemeStackCollection
{
    internal static IReadOnlyList<CardControl> Render(Control surface, Rect2 bounds,
        BoardCardPresentation[] cards, BoardRenderResult result, SpatialCardFactory faces)
    {
        var panel = new VBoxContainer { Name = "CollectionSideSchemesArea", Position = bounds.Position,
            Size = bounds.Size, CustomMinimumSize = bounds.Size, MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddThemeConstantOverride("separation", 3);
        surface.AddChild(panel);
        string caption = cards.Any(card => card.Kind == "ENVIRONMENT")
            ? cards.Any(card => card.Kind != "ENVIRONMENT") ? "SIDE SCHEMES & ENVIRONMENTS" : "ENVIRONMENTS"
            : "SIDE SCHEMES";
        panel.AddChild(new Label { Text = $"{caption} · {cards.Length}", ThemeTypeVariation = GodotThemeVariations.Caption,
            AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = Control.MouseFilterEnum.Ignore });
        var scroll = new ScrollContainer { Name = "ContinuousSideSchemesArea", FollowFocus = true,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto };
        panel.AddChild(scroll);
        var stack = new Control { Name = "SchemeStack", MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(stack);
        result.Inspector.Register(cards);
        var controls = new List<CardControl>();
        float bottom = 0;
        for (int index = 0; index < cards.Length; index++)
        {
            BoardCardPresentation card = cards[index];
            CardLayoutMetrics layout = CardControl.LayoutFor(card, CardDisplaySize.Board, faces.Scale);
            float factor = Math.Min(1, (bounds.Size.X - 18) / layout.Width);
            if (card.Kind == "ENVIRONMENT")
                factor = Math.Min(factor, (bounds.Size.Y - 26) / layout.MinimumHeight);
            CardControl control = faces.CreateFitted(card, factor);
            control.Name = $"ProceduralCard{card.TargetId}";
            control.Position = new Vector2(0, index * 64);
            control.ZIndex = index;
            stack.AddChild(control);
            PanelContainer header = EncounterStackHeader.Create(card, control.CustomMinimumSize.X);
            var overlay = new Control { Name = "EncounterStackOverlay", MouseFilter = Control.MouseFilterEnum.Ignore };
            control.AddChild(overlay);
            overlay.AddChild(header);
            if (card.TargetId is { } id) result.Register(id, control);
            result.TrackCard(control, card);
            result.UpdateRestingPose(control);
            controls.Add(control);
            bottom = control.Position.Y + control.CustomMinimumSize.Y;
        }
        stack.CustomMinimumSize = new Vector2(bounds.Size.X - 18, bottom);
        void Arrange()
        {
            float top = 0;
            float extent = 0;
            foreach (CardControl card in controls)
            {
                var header = card.FindChild("SchemeStackHeader", true, false) as Control;
                if (header is null) continue;
                card.Position = new Vector2(0, top);
                result.UpdateRestingPose(card);
                extent = top + Math.Max(card.Size.Y, header.Size.Y) * card.Scale.Y;
                top += Math.Max(64, Math.Max(header.Size.Y, header.GetCombinedMinimumSize().Y) * card.Scale.Y + 4);
            }
            stack.CustomMinimumSize = new Vector2(bounds.Size.X - 18, extent);
            stack.Size = stack.CustomMinimumSize;
            scroll.QueueSort();
            MeasureOverflow();
        }
        // The cue counts the schemes the stack does not wholly show; scrolling reveals them.
        Label overflow = OverflowCue.Create("ContinuousOverflow", 0, "Scroll down for the rest.", faces.ReadingScale,
            OverflowAxis.Down);
        overflow.MouseFilter = Control.MouseFilterEnum.Ignore;
        overflow.Text = OverflowCue.Text(bottom > bounds.Size.Y - 26 ? 1 : 0, OverflowAxis.Down);
        overflow.Visible = bottom > bounds.Size.Y - 26;
        panel.AddChild(overflow);
        void MeasureOverflow() => Callable.From(() =>
        {
            if (!InteractionControl.IsUsable(scroll)) return;
            // Each scheme's row runs from its header to the next header; the last runs to the stack's end.
            float[] tops = [.. controls.Where(InteractionControl.IsUsable).Select(card => card.Position.Y)];
            float top = scroll.ScrollVertical;
            OverflowCue.Show(overflow, OverflowCue.Hidden(tops.Select((start, index) =>
                (start, index + 1 < tops.Length ? tops[index + 1] : stack.Size.Y)), top, top + scroll.Size.Y));
        }).CallDeferred();
        scroll.GetVScrollBar().ValueChanged += _ => MeasureOverflow();
        scroll.Resized += MeasureOverflow;
        foreach (CardControl card in controls)
        {
            ((Control)card.FindChild("SchemeStackHeader", true, false)).Resized += Arrange;
            card.Resized += Arrange;
        }
        Callable.From(Arrange).CallDeferred();
        return controls;
    }
}


