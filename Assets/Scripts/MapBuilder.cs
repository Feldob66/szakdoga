using System;
using System.Collections.Generic;
using UnityEngine;

public class MapBuilder : MonoBehaviour
{
    [Header("Base Map Prefabs")]
    public GameObject startFab;
    public GameObject straightFab;
    public GameObject castleFab;
    public GameObject endFab;

    [Header("Expansion/Insert Prefabs")]
    public GameObject bumpDownWallsFab;
    public GameObject splineDefaultStraightBumpUpFab;
    public GameObject roundCornerAFab;
    public GameObject rampFab;
    public GameObject rampLargeFab;
    public GameObject rampMediumFab;
    public GameObject rampLowFab;
    public GameObject roundLargeCornerFab;
    public GameObject splideDefaultCornerLargeFab;
    public GameObject splineDefaultCornerLargeRampFab;
    public GameObject splineDefaultCornerSmallFab;
    public GameObject splineDefaultCornerSmallRampFab;
    public GameObject splineDefaultStraightHillCompleteHalfFab;
    public GameObject splineDefaultStraightHillCompleteFab;
    public GameObject splineDefaultCurveFab;
    public GameObject bumpWallsFab;
    public GameObject narrowSquareFab;
    public GameObject crestFab;
    public GameObject hillRoundFab;
    public GameObject hillSquareFab;
    public GameObject narrowBlockFab;
    public GameObject gapFab;
    public GameObject narrowRoundFab;
    public GameObject obstacleBlockFab;
    public GameObject rampSharpFab;
    public GameObject rampSquareFab;
    public GameObject sideFab;
    public GameObject splitFab;
    public GameObject splitStartFab;
    public GameObject splitWallsToOpenFab;
    public GameObject tunnelDoubleFab;
    public GameObject tunnelNarrowFab;
    public GameObject tunnelWideFab;
    public GameObject wallLeftFab;
    public GameObject wallRightFab;
    public GameObject wallsToOpenFab;
    public GameObject windmillFab;
    public GameObject squareCornerAFab;
    public GameObject skewCornerFab;
    public GameObject roundCornerBFab;
    public GameObject splineDefaultStraightBumpDownFab;
    public GameObject splineDefaultStraightFab;
    public GameObject rampSideFab;
    public GameObject rampHighFab;
    public GameObject skewLargeCornerFab;
    public GameObject rampLargeSideFab;

    [Header("Testing/Debug: Choose what to insert next!")]
    public ExpansionMode troubleTestMode = ExpansionMode.roundCornerA;

    // Context menu button to quickly test InsertTileAndShiftForward() from inspector
    [ContextMenu("Test Insert Tile and Shift")]
    public void TestInsertTileAndShift()
    {
        InsertTileAndShiftForward();
    }

    [Header("Insert Index: Choose where u like to insert next!")]
    public int insertIndex = 0;
    public ExpansionMode expandStart;
    public ExpansionMode expandEnd;

    // Randomly picks expansion modes to use for start and end expansions
    public void PickRandomExpansionModes()
    {
        Array values = Enum.GetValues(typeof(ExpansionMode));
        System.Random random = new System.Random();

        expandStart = (ExpansionMode)values.GetValue(random.Next(values.Length));
        expandEnd = (ExpansionMode)values.GetValue(random.Next(values.Length));

        // Debug.Log("expandStart: " + expandStart + ", expandEnd: " + expandEnd);
    }

    // Enum for types of expansion tiles possible
    public enum ExpansionMode
    {
        BumpDownWalls,
        SplineDefaultStraightBumpUp,
        roundCornerA,
        Ramp,
        RampLarge,
        RampMedium,
        RampLow,
        RoundLargeCorner,
        SplideDefaultCornerLarge,
        SplineDefaultCornerLargeRamp,
        SplineDefaultCornerSmall,
        SplineDefaultCornerSmallRamp,
        SplineDefaultStraightHillCompleteHalf,
        SplineDefaultStraightHillComplete,
        SplineDefaultCurve,
        BumpWalls,
        NarrowSquare,
        Crest,
        HillRound,
        HillSquare,
        NarrowBlock,
        Gap,
        NarrowRound,
        ObstacleBlock,
        RampSharp,
        RampSquare,
        Side,
        Split,
        SplitStart,
        SplitWallsToOpen,
        TunnelDouble,
        TunnelNarrow,
        TunnelWide,
        WallLeft,
        WallRight,
        WallsToOpen,
        Windmill,
        SquareCornerA,
        SkewCorner,
        RoundCornerB,
        SplineDefaultStraightBumpDown,
        SplineDefaultStraight,
        RampSide,
        RampHigh,
        SkewLargeCorner,
        RampLargeSide,
    }

    // Enum for internal map tile representation
    private enum TileKind
    {
        Start,
        Straight,
        Castle,
        End,
        BumpDownWalls,
        SplineDefaultStraightBumpUp,
        roundCornerA,
        Ramp,
        RampLarge,
        RampMedium,
        RampLow,
        RoundLargeCorner,
        SplideDefaultCornerLarge,
        SplineDefaultCornerLargeRamp,
        SplineDefaultCornerSmall,
        SplineDefaultCornerSmallRamp,
        SplineDefaultStraightHillCompleteHalf,
        SplineDefaultStraightHillComplete,
        SplineDefaultCurve,
        BumpWalls,
        NarrowSquare,
        Crest,
        HillRound,
        HillSquare,
        NarrowBlock,
        Gap,
        NarrowRound,
        ObstacleBlock,
        RampSharp,
        RampSquare,
        Side,
        Split,
        SplitStart,
        SplitWallsToOpen,
        TunnelDouble,
        TunnelNarrow,
        TunnelWide,
        WallLeft,
        WallRight,
        WallsToOpen,
        Windmill,
        SquareCornerA,
        SkewCorner,
        RoundCornerB,
        SplineDefaultStraightBumpDown,
        SplineDefaultStraight,
        RampSide,
        RampHigh,
        SkewLargeCorner,
        RampLargeSide,
    }

    [Header("Map Parent (root of built map)")]
    public Transform mapParent;

    private List<TileKind> logicalMap = new List<TileKind>();
    private List<Transform> spawnedTiles = new List<Transform>();

    // Returns the index of the last element in the logical map
    public int GetEndIndex()
    {
        return logicalMap.Count - 1;
    }

    void Start()
    {
        PickRandomExpansionModes();

        // Initialize the logical map with standard tiles
        logicalMap.Clear();
        logicalMap.Add(TileKind.Start);
        logicalMap.Add(TileKind.Straight);
        logicalMap.Add(TileKind.Castle);
        logicalMap.Add(TileKind.End);

        RebuildPhysicalMap();
    }

    // Inserts a new tile based on the troubleTestMode and insertIndex and rebuilds the map
    public void InsertTileAndShiftForward()
    {
        switch (troubleTestMode)
        {
            case ExpansionMode.roundCornerA: logicalMap.Insert(insertIndex + 1, TileKind.roundCornerA); break;
            case ExpansionMode.BumpDownWalls: logicalMap.Insert(insertIndex + 1, TileKind.BumpDownWalls); break;
            case ExpansionMode.SplineDefaultStraightBumpUp: logicalMap.Insert(insertIndex + 1, TileKind.SplineDefaultStraightBumpUp); break;
            case ExpansionMode.Ramp: logicalMap.Insert(insertIndex + 1, TileKind.Ramp); break;
            case ExpansionMode.RampLarge: logicalMap.Insert(insertIndex + 1, TileKind.RampLarge); break;
            case ExpansionMode.RampMedium: logicalMap.Insert(insertIndex + 1, TileKind.RampMedium); break;
            case ExpansionMode.RampLow: logicalMap.Insert(insertIndex + 1, TileKind.RampLow); break;
            case ExpansionMode.RoundLargeCorner: logicalMap.Insert(insertIndex + 1, TileKind.RoundLargeCorner); break;
            case ExpansionMode.SplideDefaultCornerLarge: logicalMap.Insert(insertIndex + 1, TileKind.SplideDefaultCornerLarge); break;
            case ExpansionMode.SplineDefaultCornerLargeRamp: logicalMap.Insert(insertIndex + 1, TileKind.SplineDefaultCornerLargeRamp); break;
            case ExpansionMode.SplineDefaultCornerSmall: logicalMap.Insert(insertIndex + 1, TileKind.SplineDefaultCornerSmall); break;
            case ExpansionMode.SplineDefaultCornerSmallRamp: logicalMap.Insert(insertIndex + 1, TileKind.SplineDefaultCornerSmallRamp); break;
            case ExpansionMode.SplineDefaultStraightHillCompleteHalf: logicalMap.Insert(insertIndex + 1, TileKind.SplineDefaultStraightHillCompleteHalf); break;
            case ExpansionMode.SplineDefaultStraightHillComplete: logicalMap.Insert(insertIndex + 1, TileKind.SplineDefaultStraightHillComplete); break;
            case ExpansionMode.SplineDefaultCurve: logicalMap.Insert(insertIndex + 1, TileKind.SplineDefaultCurve); break;
            case ExpansionMode.BumpWalls: logicalMap.Insert(insertIndex + 1, TileKind.BumpWalls); break;
            case ExpansionMode.NarrowSquare: logicalMap.Insert(insertIndex + 1, TileKind.NarrowSquare); break;
            case ExpansionMode.Crest: logicalMap.Insert(insertIndex + 1, TileKind.Crest); break;
            case ExpansionMode.HillRound: logicalMap.Insert(insertIndex + 1, TileKind.HillRound); break;
            case ExpansionMode.HillSquare: logicalMap.Insert(insertIndex + 1, TileKind.HillSquare); break;
            case ExpansionMode.NarrowBlock: logicalMap.Insert(insertIndex + 1, TileKind.NarrowBlock); break;
            case ExpansionMode.Gap: logicalMap.Insert(insertIndex + 1, TileKind.Gap); break;
            case ExpansionMode.NarrowRound: logicalMap.Insert(insertIndex + 1, TileKind.NarrowRound); break;
            case ExpansionMode.ObstacleBlock: logicalMap.Insert(insertIndex + 1, TileKind.ObstacleBlock); break;
            case ExpansionMode.RampSharp: logicalMap.Insert(insertIndex + 1, TileKind.RampSharp); break;
            case ExpansionMode.RampSquare: logicalMap.Insert(insertIndex + 1, TileKind.RampSquare); break;
            case ExpansionMode.Side: logicalMap.Insert(insertIndex + 1, TileKind.Side); break;
            case ExpansionMode.Split: logicalMap.Insert(insertIndex + 1, TileKind.Split); break;
            case ExpansionMode.SplitStart: logicalMap.Insert(insertIndex + 1, TileKind.SplitStart); break;
            case ExpansionMode.SplitWallsToOpen: logicalMap.Insert(insertIndex + 1, TileKind.SplitWallsToOpen); break;
            case ExpansionMode.TunnelDouble: logicalMap.Insert(insertIndex + 1, TileKind.TunnelDouble); break;
            case ExpansionMode.TunnelNarrow: logicalMap.Insert(insertIndex + 1, TileKind.TunnelNarrow); break;
            case ExpansionMode.TunnelWide: logicalMap.Insert(insertIndex + 1, TileKind.TunnelWide); break;
            case ExpansionMode.WallLeft: logicalMap.Insert(insertIndex + 1, TileKind.WallLeft); break;
            case ExpansionMode.WallRight: logicalMap.Insert(insertIndex + 1, TileKind.WallRight); break;
            case ExpansionMode.WallsToOpen: logicalMap.Insert(insertIndex + 1, TileKind.WallsToOpen); break;
            case ExpansionMode.Windmill: logicalMap.Insert(insertIndex + 1, TileKind.Windmill); break;
            case ExpansionMode.SquareCornerA: logicalMap.Insert(insertIndex + 1, TileKind.SquareCornerA); break;
            case ExpansionMode.SkewCorner: logicalMap.Insert(insertIndex + 1, TileKind.SkewCorner); break;
            case ExpansionMode.RoundCornerB: logicalMap.Insert(insertIndex + 1, TileKind.RoundCornerB); break;
            case ExpansionMode.SplineDefaultStraightBumpDown: logicalMap.Insert(insertIndex + 1, TileKind.SplineDefaultStraightBumpDown); break;
            case ExpansionMode.SplineDefaultStraight: logicalMap.Insert(insertIndex + 1, TileKind.SplineDefaultStraight); break;
            case ExpansionMode.RampSide: logicalMap.Insert(insertIndex + 1, TileKind.RampSide); break;
            case ExpansionMode.RampHigh: logicalMap.Insert(insertIndex + 1, TileKind.RampHigh); break;
            case ExpansionMode.SkewLargeCorner: logicalMap.Insert(insertIndex + 1, TileKind.SkewLargeCorner); break;
            case ExpansionMode.RampLargeSide: logicalMap.Insert(insertIndex + 1, TileKind.RampLargeSide); break;
        }

        // Rebuild the visible map after insertion change
        RebuildPhysicalMap();
    }

    // Rebuilds the physical map by destroying and respawning all tiles based on logical map states
    void RebuildPhysicalMap()
    {
        // Destroy existing spawned tiles first
        foreach (var t in spawnedTiles)
            if (t) Destroy(t.gameObject);
        spawnedTiles.Clear();

        // Initial position and rotation for tile placement
        Vector3 pos = new Vector3(0f, -1f, 0f);
        float rot = 0f;
        float tileLength = 10f;
        float elevationOffset = 0f;
        float splideCornerLargeZOffset = 0f;

        // Iterate through logical map and instantiate corresponding prefabs at calculated positions
        for (int i = 0; i < logicalMap.Count; ++i)
        {
            GameObject prefab = null;
            float thisRot = rot;

            // Special rotation for the first tile
            if (i == 0) thisRot = 180f;

            // Assign prefab based on tile type
            switch (logicalMap[i])
            {
                case TileKind.Start: prefab = startFab; break;
                case TileKind.Straight: prefab = straightFab; break;
                case TileKind.Castle: prefab = castleFab; break;
                case TileKind.End: prefab = endFab; break;
                case TileKind.roundCornerA: prefab = roundCornerAFab; break;
                case TileKind.BumpDownWalls: prefab = bumpDownWallsFab; break;
                case TileKind.SplineDefaultStraightBumpUp: prefab = splineDefaultStraightBumpUpFab; break;
                case TileKind.Ramp: prefab = rampFab; break;
                case TileKind.RampLarge: prefab = rampLargeFab; break;
                case TileKind.RampMedium: prefab = rampMediumFab; break;
                case TileKind.RampLow: prefab = rampLowFab; break;
                case TileKind.RoundLargeCorner: prefab = roundLargeCornerFab; break;
                case TileKind.SplideDefaultCornerLarge: prefab = splideDefaultCornerLargeFab; break;
                case TileKind.SplineDefaultCornerLargeRamp: prefab = splineDefaultCornerLargeRampFab; break;
                case TileKind.SplineDefaultCornerSmall: prefab = splineDefaultCornerSmallFab; break;
                case TileKind.SplineDefaultCornerSmallRamp: prefab = splineDefaultCornerSmallRampFab; break;
                case TileKind.SplineDefaultStraightHillCompleteHalf: prefab = splineDefaultStraightHillCompleteHalfFab; break;
                case TileKind.SplineDefaultStraightHillComplete: prefab = splineDefaultStraightHillCompleteFab; break;
                case TileKind.SplineDefaultCurve: prefab = splineDefaultCurveFab; break;
                case TileKind.BumpWalls: prefab = bumpWallsFab; break;
                case TileKind.NarrowSquare: prefab = narrowSquareFab; break;
                case TileKind.Crest: prefab = crestFab; break;
                case TileKind.HillRound: prefab = hillRoundFab; break;
                case TileKind.HillSquare: prefab = hillSquareFab; break;
                case TileKind.NarrowBlock: prefab = narrowBlockFab; break;
                case TileKind.Gap: prefab = gapFab; break;
                case TileKind.NarrowRound: prefab = narrowRoundFab; break;
                case TileKind.ObstacleBlock: prefab = obstacleBlockFab; break;
                case TileKind.RampSharp: prefab = rampSharpFab; break;
                case TileKind.RampSquare: prefab = rampSquareFab; break;
                case TileKind.Side: prefab = sideFab; break;
                case TileKind.Split: prefab = splitFab; break;
                case TileKind.SplitStart: prefab = splitStartFab; break;
                case TileKind.SplitWallsToOpen: prefab = splitWallsToOpenFab; break;
                case TileKind.TunnelDouble: prefab = tunnelDoubleFab; break;
                case TileKind.TunnelNarrow: prefab = tunnelNarrowFab; break;
                case TileKind.TunnelWide: prefab = tunnelWideFab; break;
                case TileKind.WallLeft: prefab = wallLeftFab; break;
                case TileKind.WallRight: prefab = wallRightFab; break;
                case TileKind.WallsToOpen: prefab = wallsToOpenFab; break;
                case TileKind.Windmill: prefab = windmillFab; break;
                case TileKind.SquareCornerA: prefab = squareCornerAFab; break;
                case TileKind.SkewCorner: prefab = skewCornerFab; break;
                case TileKind.RoundCornerB: prefab = roundCornerBFab; break;
                case TileKind.SplineDefaultStraightBumpDown: prefab = splineDefaultStraightBumpDownFab; break;
                case TileKind.SplineDefaultStraight: prefab = splineDefaultStraightFab; break;
                case TileKind.RampSide: prefab = rampSideFab; break;
                case TileKind.RampHigh: prefab = rampHighFab; break;
                case TileKind.SkewLargeCorner: prefab = skewLargeCornerFab; break;
                case TileKind.RampLargeSide: prefab = rampLargeSideFab; break;
            }

            if (!prefab) continue;  // Skip if no prefab assigned

            // Handle special cases for positioning and elevation
            if (logicalMap[i] == TileKind.SplineDefaultStraightBumpUp
                || logicalMap[i] == TileKind.SplineDefaultStraightBumpDown
                || logicalMap[i] == TileKind.SplineDefaultStraight)
            {
                Vector3 forward = Quaternion.Euler(0, rot, 0) * Vector3.forward;
                Vector3 splinePos = pos + forward * -5f + forward * splideCornerLargeZOffset;

                var t = Instantiate(prefab, splinePos + Vector3.up * elevationOffset, Quaternion.Euler(0, thisRot, 0), mapParent).transform;
                spawnedTiles.Add(t);

                pos += forward * 40f;
            }
            else if (logicalMap[i] == TileKind.SplineDefaultStraightHillCompleteHalf)
            {
                Vector3 forward = Quaternion.Euler(0, rot, 0) * Vector3.forward;
                Vector3 splinePos = pos + forward * -5f + forward * splideCornerLargeZOffset;

                var t = Instantiate(prefab, splinePos + Vector3.up * elevationOffset, Quaternion.Euler(0, thisRot, 0), mapParent).transform;
                spawnedTiles.Add(t);

                pos += forward * 40f;
                elevationOffset += 5f;
            }
            else if (logicalMap[i] == TileKind.SplineDefaultStraightHillComplete)
            {
                Vector3 forward = Quaternion.Euler(0, rot, 0) * Vector3.forward;
                Vector3 splinePos = pos + forward * -5f + forward * splideCornerLargeZOffset;

                var t = Instantiate(prefab, splinePos + Vector3.up * elevationOffset, Quaternion.Euler(0, thisRot, 0), mapParent).transform;
                spawnedTiles.Add(t);

                pos += forward * 40f;
                elevationOffset += 10f;
            }
            else if (logicalMap[i] == TileKind.RampLarge
                    || logicalMap[i] == TileKind.RampLargeSide)
            {
                Vector3 forward = Quaternion.Euler(0, rot, 0) * Vector3.forward;
                Vector3 rampLargePos = pos + forward * 5f + forward * splideCornerLargeZOffset;

                var t = Instantiate(prefab, rampLargePos + Vector3.up * elevationOffset, Quaternion.Euler(0, thisRot, 0), mapParent).transform;
                spawnedTiles.Add(t);

                pos += forward * 20f;
                elevationOffset += 5f;
            }
            else if (logicalMap[i] == TileKind.Ramp
                    || logicalMap[i] == TileKind.RampSide
                    || logicalMap[i] == TileKind.RampHigh)
            {
                var t = Instantiate(prefab, pos + Vector3.up * elevationOffset, Quaternion.Euler(0, thisRot, 0), mapParent).transform;
                spawnedTiles.Add(t);

                Vector3 moveDir = Quaternion.Euler(0, rot, 0) * Vector3.forward;

                pos += moveDir * tileLength;
                elevationOffset += 5f;
            }
            else if (logicalMap[i] == TileKind.RampMedium)
            {
                var t = Instantiate(prefab, pos + Vector3.up * elevationOffset + Quaternion.Euler(0, rot, 0) * Vector3.forward * splideCornerLargeZOffset, Quaternion.Euler(0, thisRot, 0), mapParent).transform;
                spawnedTiles.Add(t);

                Vector3 moveDir = Quaternion.Euler(0, rot, 0) * Vector3.forward;

                pos += moveDir * tileLength;
                elevationOffset += 2.5f;
            }
            else if (logicalMap[i] == TileKind.RampLow)
            {
                var t = Instantiate(prefab, pos + Vector3.up * elevationOffset + Quaternion.Euler(0, rot, 0) * Vector3.forward * splideCornerLargeZOffset, Quaternion.Euler(0, thisRot, 0), mapParent).transform;
                spawnedTiles.Add(t);

                Vector3 moveDir = Quaternion.Euler(0, rot, 0) * Vector3.forward;

                pos += moveDir * tileLength;
                elevationOffset += 1f;
            }
            else if (logicalMap[i] == TileKind.RoundLargeCorner
                    || logicalMap[i] == TileKind.SkewLargeCorner)
            {
                Vector3 forward = Quaternion.Euler(0, rot, 0) * Vector3.forward;
                Vector3 right = Quaternion.Euler(0, rot, 0) * Vector3.right;

                var t = Instantiate(prefab, pos + forward * 5f + right * 5f + Vector3.up * elevationOffset + forward * splideCornerLargeZOffset, Quaternion.Euler(0, rot, 0), mapParent).transform;
                spawnedTiles.Add(t);

                pos += forward * 10f + right * 20f;
                rot += 90f;
            }
            else if (logicalMap[i] == TileKind.SplideDefaultCornerLarge)
            {
                Vector3 forward = Quaternion.Euler(0, rot, 0) * Vector3.forward;
                Vector3 right = Quaternion.Euler(0, rot, 0) * Vector3.right;

                var t = Instantiate(prefab, pos + forward * -5f + Vector3.up * elevationOffset, Quaternion.Euler(0, rot, 0), mapParent).transform;
                spawnedTiles.Add(t);

                pos += forward * 35f + right * 45f;
                rot += 90f;
            }
            else if (logicalMap[i] == TileKind.SplineDefaultCornerLargeRamp)
            {
                Vector3 forward = Quaternion.Euler(0, rot, 0) * Vector3.forward;
                Vector3 right = Quaternion.Euler(0, rot, 0) * Vector3.right;

                var t = Instantiate(prefab, pos + forward * -5f + Vector3.up * elevationOffset, Quaternion.Euler(0, rot, 0), mapParent).transform;
                spawnedTiles.Add(t);

                pos += forward * 35f + right * 45f;
                elevationOffset += 10f;
                rot += 90f;
            }
            else if (logicalMap[i] == TileKind.SplineDefaultCornerSmall)
            {
                Vector3 forward = Quaternion.Euler(0, rot, 0) * Vector3.forward;
                Vector3 right = Quaternion.Euler(0, rot, 0) * Vector3.right;

                var t = Instantiate(prefab, pos + forward * -5f + Vector3.up * elevationOffset, Quaternion.Euler(0, rot, 0), mapParent).transform;
                spawnedTiles.Add(t);

                pos += forward * 15f + right * 25f;
                rot += 90f;
            }
            else if (logicalMap[i] == TileKind.SplineDefaultCornerSmallRamp)
            {
                Vector3 forward = Quaternion.Euler(0, rot, 0) * Vector3.forward;
                Vector3 right = Quaternion.Euler(0, rot, 0) * Vector3.right;

                var t = Instantiate(prefab, pos + forward * -5f + Vector3.up * elevationOffset, Quaternion.Euler(0, rot, 0), mapParent).transform;
                spawnedTiles.Add(t);

                pos += forward * 15f + right * 25f;
                elevationOffset += 10f;
                rot += 90f;
            }
            else if (logicalMap[i] == TileKind.SplineDefaultCurve)
            {
                Vector3 forward = Quaternion.Euler(0, rot, 0) * Vector3.forward;
                Vector3 right = Quaternion.Euler(0, rot, 0) * Vector3.right;

                var t = Instantiate(prefab, pos + forward * -5f + Vector3.up * elevationOffset, Quaternion.Euler(0, rot, 0), mapParent).transform;
                spawnedTiles.Add(t);

                pos += forward * 40f + right * 20f;
            }
            else
            {
                // Default instantiation for generic tile types
                var t = Instantiate(prefab, pos + Vector3.up * elevationOffset + Quaternion.Euler(0, rot, 0) * Vector3.forward * splideCornerLargeZOffset, Quaternion.Euler(0, thisRot, 0), mapParent).transform;
                spawnedTiles.Add(t);

                // Rotate map builder if current tile is a corner type
                if (logicalMap[i] == TileKind.roundCornerA
                    || logicalMap[i] == TileKind.SquareCornerA
                    || logicalMap[i] == TileKind.SkewCorner
                    || logicalMap[i] == TileKind.RoundCornerB)
                {
                    rot += 90f;
                }

                Vector3 moveDir = Quaternion.Euler(0, rot, 0) * Vector3.forward;
                pos += moveDir * tileLength;
            }
        }
    }
}