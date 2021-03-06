﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XRay;

using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of X-Rays
/// Created by Timwi
/// </summary>
public class XRayModule : XRayModuleBase
{
    private static readonly string[] _seed1Converter = "a1n,a1f,b1n,b1f,c1n,c1f,d1n,d1f,e1n,e1f,h2f,h2n,d7n,j1n,h6n,g1n,a6n,a2n,k2n,h1n,a7n,e2n,d6n,b3n,a10n,b10n,c10n,d10n,e10n,f10n,i10n,h9n,i9n".Split(',');

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private int[] _table;
    private int _solution;
    private SymbolInfo[] _columns;
    private SymbolInfo[] _rows;
    private SymbolInfo[] _3x3;

    static SymbolInfo convertForSeed1(int icon)
    {
        var c = _seed1Converter[icon];
        return new SymbolInfo((c[0] - 'a') + 11 * (int.Parse(c.Substring(1, c.Length - 2)) - 1), c.EndsWith("f"));
    }

    protected override void StartModule()
    {
        _moduleId = _moduleIdCounter++;

        var rnd = RuleSeedable.GetRNG();
        Debug.LogFormat("[X-Ray #{0}] Using rule seed: {1}", _moduleId, rnd.Seed);

        if (rnd.Seed == 1)
        {
            _columns = Enumerable.Range(0, 12).Select(convertForSeed1).ToArray();
            _rows = Enumerable.Range(0, 12).Select(i => convertForSeed1(i + 12)).ToArray();
            _3x3 = Enumerable.Range(0, 9).Select(i => convertForSeed1(i + 24)).ToArray();
        }
        else
        {
            // Decide on the icons for the 3×3 table up top
            _3x3 = rnd.ShuffleFisherYates(Enumerable.Range(0, 22).ToArray()).Take(9).Select(x => new SymbolInfo(x + 88, false)).ToArray();

            // For the rows, we can use any non-symmetric icon
            _rows = rnd.ShuffleFisherYates(Enumerable.Range(0, 88).ToArray()).Take(12).Select(x => new SymbolInfo(x, x < 55 ? rnd.Next(0, 2) != 0 : false)).ToArray();

            // For the columns, we can only use flippable icons that we haven’t already used for rows
            var columnsRaw = rnd.ShuffleFisherYates(Enumerable.Range(0, 55).Where(x => !_rows.Any(r => r.Index == x)).ToArray());
            _columns = new SymbolInfo[12];
            for (var i = 0; i < 6; i++)
            {
                var f = rnd.Next(0, 2);
                _columns[2 * i] = new SymbolInfo(columnsRaw[i], f == 0);
                _columns[2 * i + 1] = new SymbolInfo(columnsRaw[i], f != 0);
            }
        }

        // Generate the 12×12 grid of numbers 1–5 (still part of rule seed)
        _table = new int[144];
        var nmbrs = new List<int>();
        for (var i = 0; i < 144; i++)
        {
            nmbrs.Clear();
            var x = i % 12;
            var y = (i / 12) | 0;
            for (var j = 0; j < 5; j++)
                if ((x == 0 || j != _table[i - 1]) &&
                    (y == 0 || j != _table[i - 12]) &&
                    (x == 0 || y == 0 || j != _table[i - 13]))
                    nmbrs.Add(j);
            var n = nmbrs[rnd.Next(0, nmbrs.Count)];
            _table[i] = n;
        }

        Initialize();
    }

    private void Initialize()
    {
        var col = Rnd.Range(0, 12);
        var row = Rnd.Range(0, 12);

        // This makes sure that we don’t go off the edge of the table
        var dir = Enumerable.Range(0, 9).Where(dr => !(col == 0 && dr % 3 == 0) && !(col == 11 && dr % 3 == 2) && !(row == 0 && dr / 3 == 0) && !(row == 11 && dr / 3 == 2)).PickRandom();
        _solution = _table[(row + dir / 3 - 1) * 12 + col + (dir % 3 - 1)];

        Debug.LogFormat("[X-Ray #{0}] Column {1}, Row {2}: number there is {3}.", _moduleId, _columns[col], _rows[row], _table[row * 12 + col] + 1);
        Debug.LogFormat("[X-Ray #{0}] {1} = {2}. Solution is {3}.", _moduleId, _3x3[dir], "Move up-left,Move up,Move up-right,Move left,Stay put,Move right,Move down-left,Move down,Move down-right".Split(',')[dir], _solution + 1);

        var icons = new[] { _columns[col], _rows[row], _3x3[dir] };
        icons.Shuffle();
        var mode = (ScanningMode) Rnd.Range(0, 3);
        Debug.LogFormat("[X-Ray #{0}] Scanning {1}.", _moduleId,
            mode == ScanningMode.TopToBottom ? "from top to bottom" :
            mode == ScanningMode.BottomToTop ? "from bottom to top" : "back and forth");

        StartLights(icons, mode, ScannerColor.Green);
    }

    protected override void handleButton(int i)
    {
        if (i != _solution)
        {
            Debug.LogFormat("[X-Ray #{0}] You pressed {1}, which is wrong. Resetting module.", _moduleId, i + 1);
            Module.HandleStrike();
            Initialize();
        }
        else
        {
            Debug.LogFormat("[X-Ray #{0}] You pressed {1}. Module solved.", _moduleId, i + 1);
            MarkSolved();
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        Buttons[_solution].OnInteract();
        yield return new WaitForSeconds(.1f);
    }
}
