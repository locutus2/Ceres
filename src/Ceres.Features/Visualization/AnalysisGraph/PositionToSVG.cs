﻿#region License notice

/*
  This file is part of the Ceres project at https://github.com/dje-dev/ceres.
  Copyright (C) 2020- by David Elliott and the Ceres Authors.

  Ceres is free software under the terms of the GNU General Public License v3.0.
  You should have received a copy of the GNU General Public License
  along with Ceres. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

#region Using Directives

using System;
using System.Text;

using Ceres.Chess;

#endregion

namespace Ceres.Features.Visualization.AnalysisGraph
{
  /// <summary>
  /// Static helper methods that convert Position into an SVG image format.
  /// </summary>
  public static class PositionToSVG
  {
    // Returns an SVG representation of a specified text position.
    public static string PosSVGString(Position pos, Move move = default)
    {
      string str = BOARD;

      for (int rank = 7; rank >= 0; --rank)
      {
        for (int file = 0; file <= 7; file++)
        {
          char pChar = pos.PieceOnSquare(Square.FromFileAndRank(file, rank)).Char;//  SFPosition.PieceToChar[pos.piece_on(Types.make_square(f, r))];

          string square = "" + ("abcdefgh")[file] + "" + ("12345678")[rank];

          if (!move.IsNull && move.FromSquare.File == file && move.FromSquare.Rank == rank)
          {
            str = RewriteWithPiece(str, square, null, "yellow");
            continue;
          }

          if (pChar != '.')
          {
            string colorStr = char.IsLower(pChar) ? "black-" : "white-";
            string piece = "?";
            pChar = char.ToUpper(pChar);

            if (pChar == 'R') piece = "rook";
            else if (pChar == 'N') piece = "knight";
            else if (pChar == 'B') piece = "bishop";
            else if (pChar == 'Q') piece = "queen";
            else if (pChar == 'K') piece = "king";
            else if (pChar == 'P') piece = "pawn";
            else
              throw new Exception("unknown piece" + pChar);

            string emitStr = colorStr + piece;
            string replaceColor = null;
            if (!move.IsNull)
            {
              if (move.FromSquare.Rank == rank && move.FromSquare.File == file)
              {
                replaceColor = "red";
              }
              else if (move.ToSquare.Rank == rank && move.ToSquare.File == file)
              {
                replaceColor = "green";
              }
            }
            str = RewriteWithPiece(str, square, emitStr, replaceColor);
          }
        }
      }

      // Quotes had been replaced with "!".
      return str.Replace("!", "\"");
    }


    /// <summary>
    /// Returns completely rewritten string such that a 
    /// specified piece on a specified square is set to specified value.
    /// 
    /// TODO: Improve efficiency (avoid rewriting string fully every time).
    /// </summary>
    /// <param name="str"></param>
    /// <param name="square"></param>
    /// <param name="pieceName"></param>
    /// <param name="replaceFillColor"></param>
    /// <returns></returns>
    static string RewriteWithPiece(string str, string square, string pieceName, string replaceFillColor)
    {
      StringBuilder ret = new StringBuilder();
      string[] lines = str.Split('\n');
      foreach (string lineIter in lines)
      {
        string line = lineIter;
        if (line.Contains(" " + square + "!"))
        {
          if (replaceFillColor != null)
          {
            line = line.Replace("#d18b47", replaceFillColor);
            line = line.Replace("#ffce9e", replaceFillColor);
          }

          if (pieceName == null)
          {
            ret.Append(line + "\n");
            continue; // just rewriting color of a "from" square (now empty)
          }

          string[] tokens = line.Split(' ');
          string xStr = ""; string yStr = "";
          foreach (string token in tokens)
          {
            if (token.StartsWith("x=!")) xStr = token.Substring(3).TrimEnd('!');
            if (token.StartsWith("y=!")) yStr = token.Substring(3).TrimEnd('!');
          }

          string baseS = "<use transform = !translate(XPOS, YPOS)! xlink:href=!#PIECE! />";
          string s = baseS.Replace("XPOS", xStr).Replace("YPOS", yStr).Replace("PIECE", pieceName);

          ret.Append(line + "\n");
          ret.Append(s + "\r\n");
        }
        else
          ret.Append(line + "\n");
      }

      return ret.ToString();
    }

    /// <summary>
    /// Returns a snippet of HTML which includes SVG image of specified position.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static string GetPositionSVGInHTML(Position pos)
    {
      string svg = PosSVGString(pos);
      svg =
    @"<!DOCTYPE html>
 <html>
 <body>" + svg + "</html>";

      return svg;
    }


    // was:<svg version=!1.1! viewBox=!0 0 400 400! xmlns=!http://www.w3.org/2000/svg! xmlns:xlink=!http://www.w3.org/1999/xlink!>

    // Works when embedded wit graphviz.dot
    const string HEADER =
    @"<?xml version=!1.0! encoding=!iso-8859-1!?>
<svg version = !1.1! id=!Capa_1! xmlns=!http://www.w3.org/2000/svg! xmlns:xlink=!http://www.w3.org/1999/xlink! x=!0px! y=!0px!
	 viewBox=!0 0 400 400! style=!enable-background:new 0 0 400 400;! xml:space=!preserve!>
";

    const string BOARD = HEADER +
    @"
  <style>
    .check {
      fill: url(#check_gradient);
    }
  </style>
  <defs><g class=!white pawn! id=!white-pawn!><path d=!M22 9c-2.21 0-4 1.79-4 4 0 .89.29 1.71.78 2.38-1.95 1.12-3.28 3.21-3.28 5.62 0 2.03.94 3.84 2.41 5.03-3 1.06-7.41 5.55-7.41 13.47h23c0-7.92-4.41-12.41-7.41-13.47 1.47-1.19 2.41-3 2.41-5.03 0-2.41-1.33-4.5-3.28-5.62.49-.67.78-1.49.78-2.38 0-2.21-1.79-4-4-4z! fill=!#fff! stroke=!#000! stroke-linecap=!round! stroke-width=!1.5! /></g><g class=!white knight! fill=!none! fill-rule=!evenodd! id=!white-knight! stroke=!#000! stroke-linecap=!round! stroke-linejoin=!round! stroke-width=!1.5!><path d=!M 22,10 C 32.5,11 38.5,18 38,39 L 15,39 C 15,30 25,32.5 23,18! style=!fill:#ffffff; stroke:#000000;! /><path d=!M 24,18 C 24.38,20.91 18.45,25.37 16,27 C 13,29 13.18,31.34 11,31 C 9.958,30.06 12.41,27.96 11,28 C 10,28 11.19,29.23 10,30 C 9,30 5.997,31 6,26 C 6,24 12,14 12,14 C 12,14 13.89,12.1 14,10.5 C 13.27,9.506 13.5,8.5 13.5,7.5 C 14.5,6.5 16.5,10 16.5,10 L 18.5,10 C 18.5,10 19.28,8.008 21,7 C 22,7 22,10 22,10! style=!fill:#ffffff; stroke:#000000;! /><path d=!M 9.5 25.5 A 0.5 0.5 0 1 1 8.5,25.5 A 0.5 0.5 0 1 1 9.5 25.5 z! style=!fill:#000000; stroke:#000000;! /><path d=!M 15 15.5 A 0.5 1.5 0 1 1 14,15.5 A 0.5 1.5 0 1 1 15 15.5 z! style=!fill:#000000; stroke:#000000;! transform=!matrix(0.866,0.5,-0.5,0.866,9.693,-5.173)! /></g><g class=!white bishop! fill=!none! fill-rule=!evenodd! id=!white-bishop! stroke=!#000! stroke-linecap=!round! stroke-linejoin=!round! stroke-width=!1.5!><g fill=!#fff! stroke-linecap=!butt!><path d=!M9 36c3.39-.97 10.11.43 13.5-2 3.39 2.43 10.11 1.03 13.5 2 0 0 1.65.54 3 2-.68.97-1.65.99-3 .5-3.39-.97-10.11.46-13.5-1-3.39 1.46-10.11.03-13.5 1-1.354.49-2.323.47-3-.5 1.354-1.94 3-2 3-2zM15 32c2.5 2.5 12.5 2.5 15 0 .5-1.5 0-2 0-2 0-2.5-2.5-4-2.5-4 5.5-1.5 6-11.5-5-15.5-11 4-10.5 14-5 15.5 0 0-2.5 1.5-2.5 4 0 0-.5.5 0 2zM25 8a2.5 2.5 0 1 1-5 0 2.5 2.5 0 1 1 5 0z! /></g><path d=!M17.5 26h10M15 30h15m-7.5-14.5v5M20 18h5! stroke-linejoin=!miter! /></g><g class=!white rook! fill=!#fff! fill-rule=!evenodd! id=!white-rook! stroke=!#000! stroke-linecap=!round! stroke-linejoin=!round! stroke-width=!1.5!><path d=!M9 39h27v-3H9v3zM12 36v-4h21v4H12zM11 14V9h4v2h5V9h5v2h5V9h4v5! stroke-linecap=!butt! /><path d=!M34 14l-3 3H14l-3-3! /><path d=!M31 17v12.5H14V17! stroke-linecap=!butt! stroke-linejoin=!miter! /><path d=!M31 29.5l1.5 2.5h-20l1.5-2.5! /><path d=!M11 14h23! fill=!none! stroke-linejoin=!miter! /></g><g class=!white queen! fill=!#fff! fill-rule=!evenodd! id=!white-queen! stroke=!#000! stroke-linecap=!round! stroke-linejoin=!round! stroke-width=!1.5!><path d=!M8 12a2 2 0 1 1-4 0 2 2 0 1 1 4 0zM24.5 7.5a2 2 0 1 1-4 0 2 2 0 1 1 4 0zM41 12a2 2 0 1 1-4 0 2 2 0 1 1 4 0zM16 8.5a2 2 0 1 1-4 0 2 2 0 1 1 4 0zM33 9a2 2 0 1 1-4 0 2 2 0 1 1 4 0z! /><path d=!M9 26c8.5-1.5 21-1.5 27 0l2-12-7 11V11l-5.5 13.5-3-15-3 15-5.5-14V25L7 14l2 12zM9 26c0 2 1.5 2 2.5 4 1 1.5 1 1 .5 3.5-1.5 1-1.5 2.5-1.5 2.5-1.5 1.5.5 2.5.5 2.5 6.5 1 16.5 1 23 0 0 0 1.5-1 0-2.5 0 0 .5-1.5-1-2.5-.5-2.5-.5-2 .5-3.5 1-2 2.5-2 2.5-4-8.5-1.5-18.5-1.5-27 0z! stroke-linecap=!butt! /><path d=!M11.5 30c3.5-1 18.5-1 22 0M12 33.5c6-1 15-1 21 0! fill=!none! /></g><g class=!white king! fill=!none! fill-rule=!evenodd! id=!white-king! stroke=!#000! stroke-linecap=!round! stroke-linejoin=!round! stroke-width=!1.5!><path d=!M22.5 11.63V6M20 8h5! stroke-linejoin=!miter! /><path d=!M22.5 25s4.5-7.5 3-10.5c0 0-1-2.5-3-2.5s-3 2.5-3 2.5c-1.5 3 3 10.5 3 10.5! fill=!#fff! stroke-linecap=!butt! stroke-linejoin=!miter! /><path d=!M11.5 37c5.5 3.5 15.5 3.5 21 0v-7s9-4.5 6-10.5c-4-6.5-13.5-3.5-16 4V27v-3.5c-3.5-7.5-13-10.5-16-4-3 6 5 10 5 10V37z! fill=!#fff! /><path d=!M11.5 30c5.5-3 15.5-3 21 0m-21 3.5c5.5-3 15.5-3 21 0m-21 3.5c5.5-3 15.5-3 21 0! /></g><g class=!black pawn! id=!black-pawn!><path d=!M22 9c-2.21 0-4 1.79-4 4 0 .89.29 1.71.78 2.38-1.95 1.12-3.28 3.21-3.28 5.62 0 2.03.94 3.84 2.41 5.03-3 1.06-7.41 5.55-7.41 13.47h23c0-7.92-4.41-12.41-7.41-13.47 1.47-1.19 2.41-3 2.41-5.03 0-2.41-1.33-4.5-3.28-5.62.49-.67.78-1.49.78-2.38 0-2.21-1.79-4-4-4z! stroke=!#000! stroke-linecap=!round! stroke-width=!1.5! /></g><g class=!black knight! fill=!none! fill-rule=!evenodd! id=!black-knight! stroke=!#000! stroke-linecap=!round! stroke-linejoin=!round! stroke-width=!1.5!><path d=!M 22,10 C 32.5,11 38.5,18 38,39 L 15,39 C 15,30 25,32.5 23,18! style=!fill:#000000; stroke:#000000;! /><path d=!M 24,18 C 24.38,20.91 18.45,25.37 16,27 C 13,29 13.18,31.34 11,31 C 9.958,30.06 12.41,27.96 11,28 C 10,28 11.19,29.23 10,30 C 9,30 5.997,31 6,26 C 6,24 12,14 12,14 C 12,14 13.89,12.1 14,10.5 C 13.27,9.506 13.5,8.5 13.5,7.5 C 14.5,6.5 16.5,10 16.5,10 L 18.5,10 C 18.5,10 19.28,8.008 21,7 C 22,7 22,10 22,10! style=!fill:#000000; stroke:#000000;! /><path d=!M 9.5 25.5 A 0.5 0.5 0 1 1 8.5,25.5 A 0.5 0.5 0 1 1 9.5 25.5 z! style=!fill:#ececec; stroke:#ececec;! /><path d=!M 15 15.5 A 0.5 1.5 0 1 1 14,15.5 A 0.5 1.5 0 1 1 15 15.5 z! style=!fill:#ececec; stroke:#ececec;! transform=!matrix(0.866,0.5,-0.5,0.866,9.693,-5.173)! /><path d=!M 24.55,10.4 L 24.1,11.85 L 24.6,12 C 27.75,13 30.25,14.49 32.5,18.75 C 34.75,23.01 35.75,29.06 35.25,39 L 35.2,39.5 L 37.45,39.5 L 37.5,39 C 38,28.94 36.62,22.15 34.25,17.66 C 31.88,13.17 28.46,11.02 25.06,10.5 L 24.55,10.4 z ! style=!fill:#ececec; stroke:none;! /></g><g class=!black bishop! fill=!none! fill-rule=!evenodd! id=!black-bishop! stroke=!#000! stroke-linecap=!round! stroke-linejoin=!round! stroke-width=!1.5!><path d=!M9 36c3.39-.97 10.11.43 13.5-2 3.39 2.43 10.11 1.03 13.5 2 0 0 1.65.54 3 2-.68.97-1.65.99-3 .5-3.39-.97-10.11.46-13.5-1-3.39 1.46-10.11.03-13.5 1-1.354.49-2.323.47-3-.5 1.354-1.94 3-2 3-2zm6-4c2.5 2.5 12.5 2.5 15 0 .5-1.5 0-2 0-2 0-2.5-2.5-4-2.5-4 5.5-1.5 6-11.5-5-15.5-11 4-10.5 14-5 15.5 0 0-2.5 1.5-2.5 4 0 0-.5.5 0 2zM25 8a2.5 2.5 0 1 1-5 0 2.5 2.5 0 1 1 5 0z! fill=!#000! stroke-linecap=!butt! /><path d=!M17.5 26h10M15 30h15m-7.5-14.5v5M20 18h5! stroke=!#fff! stroke-linejoin=!miter! /></g><g class=!black rook! fill=!#000! fill-rule=!evenodd! id=!black-rook! stroke=!#000! stroke-linecap=!round! stroke-linejoin=!round! stroke-width=!1.5!><path d=!M9 39h27v-3H9v3zM12.5 32l1.5-2.5h17l1.5 2.5h-20zM12 36v-4h21v4H12z! stroke-linecap=!butt! /><path d=!M14 29.5v-13h17v13H14z! stroke-linecap=!butt! stroke-linejoin=!miter! /><path d=!M14 16.5L11 14h23l-3 2.5H14zM11 14V9h4v2h5V9h5v2h5V9h4v5H11z! stroke-linecap=!butt! /><path d=!M12 35.5h21M13 31.5h19M14 29.5h17M14 16.5h17M11 14h23! fill=!none! stroke=!#fff! stroke-linejoin=!miter! stroke-width=!1! /></g><g class=!black queen! fill=!#000! fill-rule=!evenodd! id=!black-queen! stroke=!#000! stroke-linecap=!round! stroke-linejoin=!round! stroke-width=!1.5!><g fill=!#000! stroke=!none!><circle cx=!6! cy=!12! r=!2.75! /><circle cx=!14! cy=!9! r=!2.75! /><circle cx=!22.5! cy=!8! r=!2.75! /><circle cx=!31! cy=!9! r=!2.75! /><circle cx=!39! cy=!12! r=!2.75! /></g><path d=!M9 26c8.5-1.5 21-1.5 27 0l2.5-12.5L31 25l-.3-14.1-5.2 13.6-3-14.5-3 14.5-5.2-13.6L14 25 6.5 13.5 9 26zM9 26c0 2 1.5 2 2.5 4 1 1.5 1 1 .5 3.5-1.5 1-1.5 2.5-1.5 2.5-1.5 1.5.5 2.5.5 2.5 6.5 1 16.5 1 23 0 0 0 1.5-1 0-2.5 0 0 .5-1.5-1-2.5-.5-2.5-.5-2 .5-3.5 1-2 2.5-2 2.5-4-8.5-1.5-18.5-1.5-27 0z! stroke-linecap=!butt! /><path d=!M11 38.5a35 35 1 0 0 23 0! fill=!none! stroke-linecap=!butt! /><path d=!M11 29a35 35 1 0 1 23 0M12.5 31.5h20M11.5 34.5a35 35 1 0 0 22 0M10.5 37.5a35 35 1 0 0 24 0! fill=!none! stroke=!#fff! /></g><g class=!black king! fill=!none! fill-rule=!evenodd! id=!black-king! stroke=!#000! stroke-linecap=!round! stroke-linejoin=!round! stroke-width=!1.5!><path d=!M22.5 11.63V6! stroke-linejoin=!miter! /><path d=!M22.5 25s4.5-7.5 3-10.5c0 0-1-2.5-3-2.5s-3 2.5-3 2.5c-1.5 3 3 10.5 3 10.5! fill=!#000! stroke-linecap=!butt! stroke-linejoin=!miter! /><path d=!M11.5 37c5.5 3.5 15.5 3.5 21 0v-7s9-4.5 6-10.5c-4-6.5-13.5-3.5-16 4V27v-3.5c-3.5-7.5-13-10.5-16-4-3 6 5 10 5 10V37z! fill=!#000! /><path d=!M20 8h5! stroke-linejoin=!miter! /><path d=!M32 29.5s8.5-4 6.03-9.65C34.15 14 25 18 22.5 24.5l.01 2.1-.01-2.1C20 18 9.906 14 6.997 19.85c-2.497 5.65 4.853 9 4.853 9M11.5 30c5.5-3 15.5-3 21 0m-21 3.5c5.5-3 15.5-3 21 0m-21 3.5c5.5-3 15.5-3 21 0! stroke=!#fff! /></g></defs>
  <rect class=!square dark a1! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!20! y=!335! />
  <rect class=!square light b1! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!65! y=!335! />
  <rect class=!square dark c1! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!110! y=!335! />
  <rect class=!square light d1! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!155! y=!335! />
  <rect class=!square dark e1! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!200! y=!335! />
  <rect class=!square light f1! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!245! y=!335! />
  <rect class=!square dark g1! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!290! y=!335! />
  <rect class=!square light h1! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!335! y=!335! />
  <rect class=!square light a2! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!20! y=!290! />
  <rect class=!square dark b2! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!65! y=!290! />
  <rect class=!square light c2! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!110! y=!290! />
  <rect class=!square dark d2! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!155! y=!290! />
  <rect class=!square light e2! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!200! y=!290! />
  <rect class=!square dark f2! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!245! y=!290! />
  <rect class=!square light g2! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!290! y=!290! />
  <rect class=!square dark h2! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!335! y=!290! />
  <rect class=!square dark a3! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!20! y=!245! />
  <rect class=!square light b3! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!65! y=!245! />
  <rect class=!square dark c3! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!110! y=!245! />
  <rect class=!square light d3! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!155! y=!245! />
  <rect class=!square dark e3! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!200! y=!245! />
  <rect class=!square light f3! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!245! y=!245! />
  <rect class=!square dark g3! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!290! y=!245! />
  <rect class=!square light h3! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!335! y=!245! />
  <rect class=!square light a4! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!20! y=!200! />
  <rect class=!square dark b4! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!65! y=!200! />
  <rect class=!square light c4! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!110! y=!200! />
  <rect class=!square dark d4! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!155! y=!200! />
  <rect class=!square light e4! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!200! y=!200! />
  <rect class=!square dark f4! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!245! y=!200! />
  <rect class=!square light g4! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!290! y=!200! />
  <rect class=!square dark h4! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!335! y=!200! />
  <rect class=!square dark a5! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!20! y=!155! />
  <rect class=!square light b5! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!65! y=!155! />
  <rect class=!square dark c5! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!110! y=!155! />
  <rect class=!square light d5! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!155! y=!155! />
  <rect class=!square dark e5! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!200! y=!155! />
  <rect class=!square light f5! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!245! y=!155! />
  <rect class=!square dark g5! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!290! y=!155! />
  <rect class=!square light h5! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!335! y=!155! />
  <rect class=!square light a6! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!20! y=!110! />
  <rect class=!square dark b6! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!65! y=!110! />
  <rect class=!square light c6! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!110! y=!110! />
  <rect class=!square dark d6! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!155! y=!110! />
  <rect class=!square light e6! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!200! y=!110! />
  <rect class=!square dark f6! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!245! y=!110! />
  <rect class=!square light g6! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!290! y=!110! />
  <rect class=!square dark h6! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!335! y=!110! />
  <rect class=!square dark a7! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!20! y=!65! />
  <rect class=!square light b7! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!65! y=!65! />
  <rect class=!square dark c7! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!110! y=!65! />
  <rect class=!square light d7! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!155! y=!65! />
  <rect class=!square dark e7! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!200! y=!65! />
  <rect class=!square light f7! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!245! y=!65! />
  <rect class=!square dark g7! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!290! y=!65! />
  <rect class=!square light h7! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!335! y=!65! />
  <rect class=!square light a8! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!20! y=!20! />
  <rect class=!square dark b8! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!65! y=!20! />
  <rect class=!square light c8! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!110! y=!20! />
  <rect class=!square dark d8! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!155! y=!20! />
  <rect class=!square light e8! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!200! y=!20! />
  <rect class=!square dark f8! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!245! y=!20! />
  <rect class=!square light g8! fill=!#ffce9e! height=!45! stroke=!none! width=!45! x=!290! y=!20! />
  <rect class=!square dark h8! fill=!#d18b47! height=!45! stroke=!none! width=!45! x=!335! y=!20! />
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!42! y=!10!>a</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!42! y=!390!>a</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!87! y=!10!>b</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!87! y=!390!>b</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!132! y=!10!>c</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!132! y=!390!>c</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!177! y=!10!>d</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!177! y=!390!>d</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!222! y=!10!>e</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!222! y=!390!>e</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!267! y=!10!>f</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!267! y=!390!>f</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!312! y=!10!>g</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!312! y=!390!>g</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!357! y=!10!>h</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!357! y=!390!>h</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!10! y=!357!>1</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!390! y=!357!>1</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!10! y=!312!>2</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!390! y=!312!>2</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!10! y=!267!>3</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!390! y=!267!>3</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!10! y=!222!>4</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!390! y=!222!>4</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!10! y=!177!>5</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!390! y=!177!>5</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!10! y=!132!>6</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!390! y=!132!>6</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!10! y=!87!>7</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!390! y=!87!>7</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!10! y=!42!>8</text>
  <text alignment-baseline=!middle! font-size=!14! text-anchor=!middle! x=!390! y=!42!>8</text>
</svg>
";


  }
}

