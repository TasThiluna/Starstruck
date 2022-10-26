using System;
using System.Linq;

public static class chessMoves
{
    public static string move(int pieceInfo, string cluster, char star)
    {
        switch (pieceInfo >> 1)
        {
            case 0: return rookMove(cluster, star, (pieceInfo & 1) != 0);
            case 1: return knightMove(cluster, star, (pieceInfo & 1) != 0);
            case 2: return kingMove(cluster, star, (pieceInfo & 1) != 0);
            case 3: return bishopMove(cluster, star, (pieceInfo & 1) != 0);
            case 4: return queenMove(cluster, star, (pieceInfo & 1) != 0);
            default: throw new InvalidOperationException(string.Format("Invalid piece info: {0}", pieceInfo));
        }
    }

    public static string rookMove(string cluster, char star, bool invert = false)
    {
        var result = "";
        var mainIx = cluster.IndexOf(star);
        foreach (var str in cluster)
        {
            if (str == '█')
                continue;
            else if (str == star)
                continue;
            var ix = cluster.IndexOf(str);
            if (ix % 8 == mainIx % 8 || ix / 8 == mainIx / 8)
                result += str;
        }
        if (!invert)
            return result;
        else
            return cluster.Where(s => s != star && s != '█' && !result.Contains(s)).Join("");
    }

    public static string knightMove(string cluster, char star, bool invert = false)
    {
        var result = "";
        var ix = cluster.IndexOf(star);
        if (ix / 8 != 0 && ix / 8 != 1)
        {
            if (ix % 8 != 0)
                result += cluster[ix - 17];
            if (ix % 8 != 7)
                result += cluster[ix - 15];
        }
        if (ix / 8 != 6 && ix / 8 != 7)
        {
            if (ix % 8 != 0)
                result += cluster[ix + 15];
            if (ix % 8 != 7)
                result += cluster[ix + 17];
        }
        if (ix % 8 != 0 && ix % 8 != 1)
        {
            if (ix / 8 != 0)
                result += cluster[ix - 10];
            if (ix / 8 != 7)
                result += cluster[ix + 6];
        }
        if (ix % 8 != 6 && ix % 8 != 7)
        {
            if (ix / 8 != 0)
                result += cluster[ix - 6];
            if (ix / 8 != 7)
                result += cluster[ix + 10];
        }
        result = new string(result.Where(ch => ch != '█' && ch != star).ToArray());
        if (!invert)
            return result;
        else
            return cluster.Where(s => s != star && s != '█' && !result.Contains(s)).Join("");
    }

    public static string kingMove(string cluster, char star, bool invert = false)
    {
        var result = "";
        var ix = cluster.IndexOf(star);
        var leftBorder = ix % 8 == 0;
        var rightBorder = ix % 8 == 7;
        var upBorder = ix / 8 == 0;
        var downBorder = ix / 8 == 7;
        if (!leftBorder && !upBorder)
            result += cluster[ix - 9];
        if (!rightBorder && !upBorder)
            result += cluster[ix - 7];
        if (!leftBorder && !downBorder)
            result += cluster[ix + 7];
        if (!rightBorder && !downBorder)
            result += cluster[ix + 9];
        if (!leftBorder)
            result += cluster[ix - 1];
        if (!rightBorder)
            result += cluster[ix + 1];
        if (!upBorder)
            result += cluster[ix - 8];
        if (!downBorder)
            result += cluster[ix + 8];
        result = new string(result.Where(ch => ch != '█' && ch != star).ToArray());
        if (!invert)
            return result;
        else
            return cluster.Where(s => s != star && s != '█' && !result.Contains(s)).Join("");
    }

    public static string bishopMove(string cluster, char star, bool invert = false)
    {
        var result = "";
        var mainIx = cluster.IndexOf(star);
        var offsets = new[] { -9, -7, 7, 9 }; // UL, UR, DL, DR
        for (int i = 0; i < 4; i++)
        {
            var space = mainIx;
            Predicate<int> horiLoopCheck;
            if (i == 0 || i == 2)
                horiLoopCheck = (x => x % 8 <= mainIx % 8);
            else
                horiLoopCheck = (x => x % 8 >= mainIx % 8);
            Predicate<int> vertLoopCheck;
            if (i == 0 || i == 1)
                vertLoopCheck = (x => x / 8 <= mainIx / 8);
            else
                vertLoopCheck = (x => x / 8 >= mainIx / 8);
            var firstTime = true;
            while (horiLoopCheck(space) && vertLoopCheck(space) && space >= 0 && space < 64)
            {
                if (!firstTime && cluster[space] != '█')
                    result += cluster[space];
                firstTime = false;
                space += offsets[i];
            }
        }
        if (!invert)
            return result;
        else
            return cluster.Where(s => s != star && s != '█' && !result.Contains(s)).Join("");
    }

    public static string queenMove(string cluster, char star, bool invert = false)
    {
        var combo = rookMove(cluster, star) + bishopMove(cluster, star);
        if (!invert)
            return combo;
        else
            return cluster.Where(s => s != star && s != '█' && !combo.Contains(s)).Join("");
    }

}