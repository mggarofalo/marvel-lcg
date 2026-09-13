using System.Buffers.Binary;
using System.Text.Json;
using Godot;

namespace Marvel.Godot;

/// <summary>Provides optional illustration textures for already-visible card faces.</summary>
public interface ICardArtProvider
{
    /// <summary>Returns local art for a visible stable face id, or null for fallback.</summary>
    Texture2D? Find(string faceId);
}
