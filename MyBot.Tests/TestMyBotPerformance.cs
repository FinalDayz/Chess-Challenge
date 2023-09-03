using System.Diagnostics;
using ChessChallenge.Chess;
using Xunit.Abstractions;

public class UnitTest1 {

  public static ITestOutputHelper _output;


  public UnitTest1(ITestOutputHelper output) {
    _output = output;
  }

  [Theory(DisplayName = "Has to move or defend the knight")]
  [InlineData(2000)]
  [InlineData(1000)]
  [InlineData(750)]
  [InlineData(500)]
  [InlineData(250)]
  [InlineData(100)]
  public void Test_dont_knight(int maxTimeToTake) {
    Console.Out.WriteLine("============");
    PositionTester
    .testPosition("r1b1kbnr/pp1ppppp/2n3q1/2Q5/4N3/4P3/PPPP1PPP/R1B1KBNR w KQkq - 1 6")
    .setOnlyGoodMoves(new String[] { "Qd5", "Ng3", "d3", "Bd3", "f3", "Nc3", "Qc4" })
    .setMaxTimeToTake(maxTimeToTake)
    .test();
  }

  [Theory(DisplayName = "Queen can be blundered, but hxg4 also blunders a bishop")]
  [InlineData(2000)]
  [InlineData(1000)]
  [InlineData(750)]
  [InlineData(500)]
  [InlineData(250)]
  [InlineData(0, 144350)]
  public void Test1(int maxTimeToTake, int evaluations = 0) {
    Console.Out.WriteLine("============");
    PositionTester
    .testPosition("r6r/pp1k2b1/2ppp1p1/3n1pp1/PP1P2bP/2PQ2PB/4PP2/2R1K2R w - - 0 21")
    .setOnlyBadMoves(new String[] { "hxg5", "Qe4", "Qxf5", "Qe3", "Qf3", "Qb5", "Qa5" })
    .setMaxTimeToTake(maxTimeToTake)
    .setMaxEvaluations(evaluations)
    .test();
  }


  [Theory(DisplayName = "Looks like black can capture bishop and rook for knight, but leads to checkmate")]
  [InlineData(2000)]
  [InlineData(1000)]
  [InlineData(750)]
  [InlineData(500)]
  [InlineData(250)]
  [InlineData(0, 144350)]
  public void Test_Looks_like_free_piece_but_leads_to_checkmate(int maxTimeToTake, int evaluations = 0) {
    Console.Out.WriteLine("============");
    PositionTester
    .testPosition("7k/rb3p1p/pppb1Np1/3pB1P1/P7/1P1P1P2/1RPRnPK1/8 b - - 0 1")
    .setOnlyGoodMoves(new String[] { "Nf4+" })
    .setMaxTimeToTake(maxTimeToTake)
    .setMaxEvaluations(evaluations)
    .test();
  }

  [Theory(DisplayName = "Has to move the queen or remove pawh that attacks queen")]
  [InlineData(2000)]
  [InlineData(1000)]
  [InlineData(750)]
  [InlineData(500)]
  [InlineData(250)]
  [InlineData(100)]
  public void Test_dont_blunde_queen(int maxTimeToTake) {
    Console.Out.WriteLine("============");
    PositionTester
    .testPosition("1r3rk1/p3ppbp/b2p1np1/2pP1q2/2P1P3/BP3NP1/2KN1PBP/7R b - e3 0 17")
    .setOnlyGoodMoves(new String[] { "Nxe4", "Qd7", "Qc8", "Qg4", "Qh5" })
    .setMaxTimeToTake(maxTimeToTake)
    .test();
  }

  [Theory(DisplayName = "Has to move the queen")]
  [InlineData(2000)]
  [InlineData(1000)]
  [InlineData(750)]
  [InlineData(500)]
  [InlineData(250)]
  [InlineData(100)]
  public void Test_dont_blunde_quee_2(int maxTimeToTake) {
    Console.Out.WriteLine("============");
    PositionTester
    .testPosition("r3k2r/p1q1np1p/np2p1p1/2ppP1NQ/3P4/P1P1B3/2P2PPP/R3K2R w KQkq - 0 12")
    .setOnlyGoodMoves(new String[] { "Qf3", "Qd1", "Qe2", "Qh3", "Qg4", "Qh4", "Qh6" })
    .setMaxTimeToTake(maxTimeToTake)
    .test();
  }
  //
  //

  class PositionTester {
    public string fen;
    private MyTestBot bot;
    public String[]? badMoves = null;
    public String[]? goodMoves = null;

    private PositionTester(String fen) {
      this.fen = fen;
      bot = new MyTestBot();
    }

    public PositionTester setOnlyGoodMoves(String[] moves) {
      goodMoves = moves;
      badMoves = null;
      return this;
    }

    public PositionTester setOnlyBadMoves(String[] moves) {
      badMoves = moves;
      goodMoves = null;
      return this;
    }

    public PositionTester setMaxTimeToTake(int maxTime) {
      bot.setTimeToThink(maxTime);

      return this;
    }

    public PositionTester setMaxDepth(int depth) {
      bot.setDepth(depth);

      return this;
    }

    public void test() {
      Board board = new Board();
      board.LoadPosition(fen);

      ChessChallenge.API.Board brd = new(board);

      ChessChallenge.API.Move move = bot.Think(
        new(board),
        new ChessChallenge.API.Timer(60000, 60000, 60000)
        );

      String moveStr = move.toSANString(board);

      if (goodMoves != null) {
        Assert.True(goodMoves.Contains(moveStr), " Bot played move '" + moveStr + "', but is not in the move list with FEN '" + fen + "'");
      }

      if (badMoves != null) {
        Assert.False(badMoves.Contains(moveStr), " Bot played move '" + moveStr + "', but it is in the bad move list with FEN '" + fen + "'");
      }
    }

    public static PositionTester testPosition(String fen) {
      return new PositionTester(fen);
    }

    public PositionTester setMaxEvaluations(int evaluations) {
      bot.setMaxEvaluations(evaluations);
      return this;
    }
  }

  class MyTestBot : MyBot {

    int timeToThink = 0;
    public int maxDepth = 0;
    private int maxEvaluations = 0;

    public void setTimeToThink(int timeToThink) {
      this.timeToThink = timeToThink;
    }

    public void setDepth(int depth) {
      this.maxDepth = depth;
    }

    public ChessChallenge.API.Move Think(ChessChallenge.API.Board board, ChessChallenge.API.Timer timer) {

      MyBot.timer = timer;
      TIMEOUT_CHECKS = 0;

      Node rootNode = new Node(ChessChallenge.API.Move.NullMove, board.IsWhiteToMove ? 1 : -1, board, "", null);

      if (timeToThink != 0) {
        whenToStop = timeToThink;
        maxDepth = 99;
      } else if (maxDepth != 0) {
        whenToStop = 10 * 60 * 1000;
      } else if (maxEvaluations != 0) {
        MAX_TIMEOUT_CHECKS = maxEvaluations;
        whenToStop = 10 * 60 * 1000;
        maxDepth = 99;
      } else {
        maxDepth = 99;
        whenToStop = calculateWhenToStop(timer);
      }

      Console.WriteLine("whenToStop: " + whenToStop + " (MAX_POSITIONS_EVALUATED: " + MAX_TIMEOUT_CHECKS + ")");

      for (int i = 1; i <= maxDepth; i++) {
        Console.WriteLine("Calculating depth: " + i);
        rootNode.negaMax(i, board, 0, float.MinValue, float.MaxValue, false);

        if (timer.MillisecondsElapsedThisTurn * 1.3 >= whenToStop) break;
        if (shouldStop()) break;

        if (rootNode.moveScore > 99990000) {
          break;
        }
      }

      Console.WriteLine("POSITIONS_EVALUATED_BEFORE_TIMEOUT: " + TIMEOUT_CHECKS_BEFORE_TIMEOUT + " (" + TIMEOUT_CHECKS + ")");
      Console.WriteLine("That took: " + timer.MillisecondsElapsedThisTurn + " ms");

      return rootNode.bestMove;
    }

    internal void setMaxEvaluations(int evaluations) {
      this.maxEvaluations = evaluations;
    }

    override public bool shouldStop() {
      return TIMEOUT_CHECKS >= MAX_TIMEOUT_CHECKS;
    }
  }
}
