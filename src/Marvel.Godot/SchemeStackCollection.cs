using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Overlapping landscape schemes expose a full-width inspectable header for every copy.</summary>
internal static class SchemeStackCollection
{
    internal static IReadOnlyList<CardControl> Render(Control surface, Rect2 bounds,
        BoardCardPresentation[] cards, BoardRenderResult result, InterfaceScale scale, ICardArtProvider? art)
    {
        var panel = new VBoxContainer { Name = "CollectionSideSchemesArea", Position = bounds.Position,
            Size = bounds.Size, CustomMinimumSize = bounds.Size, MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddThemeConstantOverride("separation", 3);
        surface.AddChild(panel);
        panel.AddChild(new Label { Text = $"SIDE SCHEMES · {cards.Length}", ThemeTypeVariation = GodotThemeVariations.Caption,
            MouseFilter = Control.MouseFilterEnum.Ignore });
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
            CardControl control = CardControl.Create(card, CardDisplaySize.Board, scale, art);
            float factor = Math.Min(1, (bounds.Size.X - 18) / control.CustomMinimumSize.X);
            control.Name = $"ProceduralCard{card.TargetId}";
            control.Position = new Vector2(0, index * 64);
            control.Scale = Vector2.One * factor;
            control.ZIndex = index;
            stack.AddChild(control);
            var header = new PanelContainer { Name = "SchemeStackHeader", Position = Vector2.Zero,
                Size = new Vector2(control.CustomMinimumSize.X, 60 / factor),
                MouseFilter = Control.MouseFilterEnum.Ignore };
            using var ink = new StyleBoxFlat { BgColor = CardFaceStyle.Ink,
                ContentMarginLeft = 7, ContentMarginRight = 7, ContentMarginTop = 3, ContentMarginBottom = 3 };
            header.AddThemeStyleboxOverride("panel", ink);
            var copy = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            copy.AddThemeConstantOverride("separation", 0);
            header.AddChild(copy);
            Label title = CardSourceStrip.Label(card.Title, "StackTitle", 14, CardTypography.Title);
            title.AddThemeColorOverride("font_color", CardFaceStyle.Paper);
            copy.AddChild(title);
            if (CardProgressValue.From(card) is { IsThreat: true } threat)
            {
                Label value = CardSourceStrip.Label($"Threat {threat.Value}", "StackThreat", 11, CardTypography.Body);
                value.AddThemeColorOverride("font_color", CardFaceStyle.Paper);
                copy.AddChild(value);
            }
            string statuses = string.Join(" · ", CardStatusEntries.From(card).Select(entry => entry.Text));
            if (statuses.Length > 0)
            {
                Label badges = CardSourceStrip.Label(statuses, "StackStatuses", 11, CardTypography.Body);
                badges.AddThemeColorOverride("font_color", CardFaceStyle.Paper);
                badges.AutowrapMode = TextServer.AutowrapMode.Word;
                copy.AddChild(badges);
            }
            var overlay = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
            control.AddChild(overlay);
            overlay.AddChild(header);
            if (card.TargetId is { } id) result.Register(id, control);
            result.TrackCard(control, card);
            result.UpdateRestingPose(control);
            controls.Add(control);
            bottom = control.Position.Y + control.CustomMinimumSize.Y * factor;
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
        }
        foreach (CardControl card in controls)
            ((Control)card.FindChild("SchemeStackHeader", true, false)).Resized += Arrange;
        Callable.From(Arrange).CallDeferred();
        if (bottom > bounds.Size.Y - 26) panel.AddChild(new Label { Name = "ContinuousOverflow", Text = "Scroll to see all ↓",
            ThemeTypeVariation = GodotThemeVariations.Caption, MouseFilter = Control.MouseFilterEnum.Ignore });
        return controls;
    }
}


