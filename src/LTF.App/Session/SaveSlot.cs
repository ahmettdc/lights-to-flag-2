using System;

namespace LTF.App.Session;

/// <summary>
/// A discovered save on disk: which carset it belongs to, its slot name, and lightweight metadata for the
/// load-game list. The carset id lives in the filename because a <c>CareerState</c> save doesn't carry it;
/// <see cref="LastSavedUtc"/> is the file's timestamp — display-only metadata, never game state.
/// </summary>
public sealed record SaveSlot(
    string CarsetId,
    string Slot,
    string SavePath,
    string TeamName,
    string TeamBadge,
    DateOnly Date,
    DateTime LastSavedUtc);
