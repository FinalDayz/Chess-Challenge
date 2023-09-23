using System;
using System.Collections.Generic;
using System.Linq;
using Stockfish.NET;
using Stockfish.NET.Models;

namespace ChessChallenge.benchmark {
  static class StockfishTester {
    public static void StartBenchmark() {
      Console.WriteLine("Test");


      // foreach (KeyValuePair<string, string> item in (new CustomSettings()).GetPropertiesAsDictionary()) {
      //   Console.WriteLine("setoption name " + item.Key + " value " + item.Value, new CustomSettings());
      // }

      // IStockfish stockfish = new Stockfish.NET.Stockfish("/Users/stefandamen/Documents/GitHub/Chess-Challenge/Chess-Challenge/stockfish", 99, new CustomSettings());
      // stockfish.SetPosition("e2e4", "e7e6");
      // Console.WriteLine(stockfish.GetBestMove());

    }
  }
}