using ChessChallenge.Chess;
using ChessChallenge.Example;
using Raylib_cs;
using System;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ChessChallenge.Application.Settings;
using static ChessChallenge.Application.ConsoleHelper;

namespace ChessChallenge.Application
{
  public class ChallengeController
  {
    public enum PlayerType
    {
      Human,
      MyBot,
      EvilBot
    }

    // Game state
    readonly Random rng;
    int gameID;
    bool isPlaying;
    Board board;
    public ChessPlayer PlayerWhite { get; private set; }
    public ChessPlayer PlayerBlack { get; private set; }

    float lastMoveMadeTime;
    bool isWaitingToPlayMove;
    Move moveToPlay;
    float playMoveTime;
    public bool HumanWasWhiteLastGame { get; private set; }

    // Bot match state
    readonly string[] botMatchStartFens;
    int botMatchGameIndex;
    public BotMatchStats BotStatsA { get; private set; }
    public BotMatchStats BotStatsB { get; private set; }
    bool botAPlaysWhite;


    // Bot task
    AutoResetEvent botTaskWaitHandle;
    bool hasBotTaskException;
    ExceptionDispatchInfo botExInfo;

    // Other
    readonly BoardUI boardUI;
    readonly MoveGenerator moveGenerator;
    readonly int tokenCount;
    readonly int debugTokenCount;
    readonly StringBuilder pgns;

    public ChallengeController()
    {
      Log($"Launching Chess-Challenge version {Settings.Version}");
      (tokenCount, debugTokenCount) = GetTokenCount();
      Warmer.Warm();

      rng = new Random();
      moveGenerator = new();
      boardUI = new BoardUI();
      board = new Board();
      pgns = new();


      BotStatsA = new BotMatchStats("IBot");
      BotStatsB = new BotMatchStats("IBot");
      botMatchStartFens = FileHelper.ReadResourceFile("Fens.txt").Split('\n').Where(fen => fen.Length > 0).ToArray();
      botTaskWaitHandle = new AutoResetEvent(false);

      // StartNewGame(PlayerType.Human, PlayerType.MyBot); // Player as white
      StartNewGame(PlayerType.MyBot, PlayerType.Human); // Player as black
    }

    public void StartNewGame(PlayerType whiteType, PlayerType blackType)
    {
      moveHistory = "";
      // End any ongoing game
      EndGame(GameResult.DrawByArbiter, log: false, autoStartNextBotMatch: false);
      gameID = rng.Next();

      // Stop prev task and create a new one
      if (RunBotsOnSeparateThread)
      {
        // Allow task to terminate
        botTaskWaitHandle.Set();
        // Create new task
        botTaskWaitHandle = new AutoResetEvent(false);
        Task.Factory.StartNew(BotThinkerThread, TaskCreationOptions.LongRunning);
      }
      // Board Setup
      board = new Board();
      bool isGameWithHuman = whiteType is PlayerType.Human || blackType is PlayerType.Human;
      int fenIndex = isGameWithHuman ? 0 : botMatchGameIndex / 2;
      board.LoadPosition(botMatchStartFens[fenIndex]);



      // Blunders the queen in 1 move...
      // board.LoadPosition("r6r/pp1k2b1/2ppp1p1/3n1pp1/PP1P2bP/2PQ2PB/4PP2/2R1K2R w - - 0 21");

      // Taking bishop and rook is actually causing checkmate
      // board.LoadPosition("7k/rb3p1p/pppb1Np1/3pB1P1/P7/1P1P1P2/1RPRnPK1/8 b - - 0 1");

      // Blunders queen indirectly by attacking the opponent queen with knight
      // board.LoadPosition("6k1/2rn1pp1/1p1bp2p/p3N3/Q2PqP2/8/1P3PBP/3R2K1 b - - 9 27");

      // Has mate, but is only procastenating (black mate)
      // board.LoadPosition("3r2n1/5r2/8/1K6/8/8/8/q1kq4 b - - 90 156");
      // Has mate, but is only procastenating (white mate)
      // board.LoadPosition("6k1/6P1/6KP/8/8/8/8/8 w - - 0 1");

      // board.LoadPosition("rn2kb1r/pp2p3/2ppBn2/5pBp/3P4/2NQP3/PqP2PPP/R3K2R w KQq - 0 12");
      // Same as above, but player can attack queen in 1 move
      // board.LoadPosition("rn1k1b1r/pp2p3/2ppBn2/5pBp/3P4/2NQP3/PqP2PPP/R4RK1 w - - 2 13");

      // Knigh captures pawn results in 1 move queen blunder
      // board.LoadPosition("r2q1rk1/pp1nbpp1/5n1p/2Pp4/8/2NQPN2/PP3PPB/3R1RK1 b - - 0 15");

      //
      // board.LoadPosition("rn2kbnr/p3p2p/b2pNpp1/q7/4PB2/1BQ2N2/PP3PPP/2R2RK1 w k - 0 18");

      // If f3 is played, bot blunders knight (1098ms to move)
      // board.LoadPosition("4k3/p3p2p/2npNppn/8/4P3/1B1b4/PP1N1PPP/2R3K1 w - - 8 28");

      // If knight is moved for fried liver, it blunders
      // board.LoadPosition("r4b1r/ppp1npp1/4kn1p/3pNp2/2PP1P2/N3P2P/PP4P1/1RB2RK1 w - - 2 16");



      // Player Setup
      PlayerWhite = CreatePlayer(whiteType);
      PlayerBlack = CreatePlayer(blackType);
      PlayerWhite.SubscribeToMoveChosenEventIfHuman(OnMoveChosen);
      PlayerBlack.SubscribeToMoveChosenEventIfHuman(OnMoveChosen);

      // UI Setup§
      boardUI.UpdatePosition(board);
      boardUI.ResetSquareColours();
      SetBoardPerspective();

      // Start
      isPlaying = true;
      NotifyTurnToMove();
    }

    void BotThinkerThread()
    {
      int threadID = gameID;
      //Console.WriteLine("Starting thread: " + threadID);

      while (true)
      {
        // Sleep thread until notified
        botTaskWaitHandle.WaitOne();
        // Get bot move
        if (threadID == gameID)
        {
          var move = GetBotMove();

          if (threadID == gameID)
          {
            OnMoveChosen(move);
          }
        }
        // Terminate if no longer playing this game
        if (threadID != gameID)
        {
          break;
        }
      }
      //Console.WriteLine("Exitting thread: " + threadID);
    }

    Move GetBotMove()
    {
      API.Board botBoard = new(board);
      try
      {
        API.Timer timer = new(PlayerToMove.TimeRemainingMs, PlayerNotOnMove.TimeRemainingMs, GameDurationMilliseconds, IncrementMilliseconds);
        API.Move move = PlayerToMove.Bot.Think(botBoard, timer);
        return new Move(move.RawValue);
      }
      catch (Exception e)
      {
        Log("An error occurred while bot was thinking.\n" + e.ToString(), true, ConsoleColor.Red);
        hasBotTaskException = true;
        botExInfo = ExceptionDispatchInfo.Capture(e);
      }
      return Move.NullMove;
    }

    public String moveHistory = "";

    void NotifyTurnToMove()
    {

      if (board.AllGameMoves.Count > 0)
      {
        Move lastMove = board.AllGameMoves.Last();
        board.UndoMove(lastMove);
        String moveStr = MoveUtility.GetMoveNameSAN(lastMove, board);
        board.MakeMove(lastMove);
        moveHistory += moveStr + " ";
        Console.WriteLine(board.AllGameMoves.Count + "]:: " + moveHistory);
      }

      //playerToMove.NotifyTurnToMove(board);
      if (PlayerToMove.IsHuman)
      {
        PlayerToMove.Human.SetPosition(FenUtility.CurrentFen(board));
        PlayerToMove.Human.NotifyTurnToMove();
      }
      else
      {
        if (RunBotsOnSeparateThread)
        {
          botTaskWaitHandle.Set();
        }
        else
        {
          double startThinkTime = Raylib.GetTime();
          var move = GetBotMove();
          double thinkDuration = Raylib.GetTime() - startThinkTime;
          PlayerToMove.UpdateClock(thinkDuration);
          OnMoveChosen(move);
        }
      }
    }

    void SetBoardPerspective()
    {
      // Board perspective
      if (PlayerWhite.IsHuman || PlayerBlack.IsHuman)
      {
        boardUI.SetPerspective(PlayerWhite.IsHuman);
        HumanWasWhiteLastGame = PlayerWhite.IsHuman;
      }
      else if (PlayerWhite.Bot is MyBot && PlayerBlack.Bot is MyBot)
      {
        boardUI.SetPerspective(true);
      }
      else
      {
        boardUI.SetPerspective(PlayerWhite.Bot is MyBot);
      }
    }

    ChessPlayer CreatePlayer(PlayerType type)
    {
      return type switch
      {
        PlayerType.MyBot => new ChessPlayer(new MyBot(), type, GameDurationMilliseconds),
        PlayerType.EvilBot => new ChessPlayer(new EvilBot(), type, GameDurationMilliseconds),
        _ => new ChessPlayer(new HumanPlayer(boardUI), type)
      };
    }

    static (int totalTokenCount, int debugTokenCount) GetTokenCount()
    {
      string path = Path.Combine(Directory.GetCurrentDirectory(), "src", "My Bot", "MyBot.cs");

      using StreamReader reader = new(path);
      string txt = reader.ReadToEnd();
      return TokenCounter.CountTokens(txt);
    }

    void OnMoveChosen(Move chosenMove)
    {
      if (IsLegal(chosenMove))
      {
        PlayerToMove.AddIncrement(IncrementMilliseconds);
        if (PlayerToMove.IsBot)
        {
          moveToPlay = chosenMove;
          isWaitingToPlayMove = true;
          playMoveTime = lastMoveMadeTime + MinMoveDelay;
        }
        else
        {
          PlayMove(chosenMove);
        }
      }
      else
      {
        string moveName = MoveUtility.GetMoveNameUCI(chosenMove);
        string log = $"Illegal move: {moveName} in position: {FenUtility.CurrentFen(board)}";
        Log(log, true, ConsoleColor.Red);
        GameResult result = PlayerToMove == PlayerWhite ? GameResult.WhiteIllegalMove : GameResult.BlackIllegalMove;
        EndGame(result);
      }
    }

    void PlayMove(Move move)
    {
      if (isPlaying)
      {
        bool animate = PlayerToMove.IsBot;
        lastMoveMadeTime = (float)Raylib.GetTime();

        board.MakeMove(move, false);
        boardUI.UpdatePosition(board, move, animate);

        GameResult result = Arbiter.GetGameState(board);
        if (result == GameResult.InProgress)
        {
          NotifyTurnToMove();
        }
        else
        {
          EndGame(result);
        }
      }
    }

    void EndGame(GameResult result, bool log = true, bool autoStartNextBotMatch = true)
    {
      if (isPlaying)
      {
        isPlaying = false;
        isWaitingToPlayMove = false;
        gameID = -1;

        if (log)
        {
          Log("Game Over: " + result, false, ConsoleColor.Blue);
        }

        string pgn = PGNCreator.CreatePGN(board, result, GetPlayerName(PlayerWhite), GetPlayerName(PlayerBlack));
        pgns.AppendLine(pgn);

        // If 2 bots playing each other, start next game automatically.
        if (PlayerWhite.IsBot && PlayerBlack.IsBot)
        {
          UpdateBotMatchStats(result);
          botMatchGameIndex++;
          int numGamesToPlay = botMatchStartFens.Length * 2;

          if (botMatchGameIndex < numGamesToPlay && autoStartNextBotMatch)
          {
            botAPlaysWhite = !botAPlaysWhite;
            const int startNextGameDelayMs = 600;
            System.Timers.Timer autoNextTimer = new(startNextGameDelayMs);
            int originalGameID = gameID;
            autoNextTimer.Elapsed += (s, e) => AutoStartNextBotMatchGame(originalGameID, autoNextTimer);
            autoNextTimer.AutoReset = false;
            autoNextTimer.Start();

          }
          else if (autoStartNextBotMatch)
          {
            Log("Match finished", false, ConsoleColor.Blue);
          }
        }
      }
    }

    private void AutoStartNextBotMatchGame(int originalGameID, System.Timers.Timer timer)
    {
      if (originalGameID == gameID)
      {
        StartNewGame(PlayerBlack.PlayerType, PlayerWhite.PlayerType);
      }
      timer.Close();
    }


    void UpdateBotMatchStats(GameResult result)
    {
      UpdateStats(BotStatsA, botAPlaysWhite);
      UpdateStats(BotStatsB, !botAPlaysWhite);

      void UpdateStats(BotMatchStats stats, bool isWhiteStats)
      {
        // Draw
        if (Arbiter.IsDrawResult(result))
        {
          stats.NumDraws++;
        }
        // Win
        else if (Arbiter.IsWhiteWinsResult(result) == isWhiteStats)
        {
          stats.NumWins++;
        }
        // Loss
        else
        {
          stats.NumLosses++;
          stats.NumTimeouts += (result is GameResult.WhiteTimeout or GameResult.BlackTimeout) ? 1 : 0;
          stats.NumIllegalMoves += (result is GameResult.WhiteIllegalMove or GameResult.BlackIllegalMove) ? 1 : 0;
        }
      }
    }

    public void Update()
    {
      if (isPlaying)
      {
        PlayerWhite.Update();
        PlayerBlack.Update();

        PlayerToMove.UpdateClock(Raylib.GetFrameTime());
        if (PlayerToMove.TimeRemainingMs <= 0)
        {
          EndGame(PlayerToMove == PlayerWhite ? GameResult.WhiteTimeout : GameResult.BlackTimeout);
        }
        else
        {
          if (isWaitingToPlayMove && Raylib.GetTime() > playMoveTime)
          {
            isWaitingToPlayMove = false;
            PlayMove(moveToPlay);
          }
        }
      }

      if (hasBotTaskException)
      {
        hasBotTaskException = false;
        botExInfo.Throw();
      }
    }

    public void Draw()
    {
      boardUI.Draw();
      string nameW = GetPlayerName(PlayerWhite);
      string nameB = GetPlayerName(PlayerBlack);
      boardUI.DrawPlayerNames(nameW, nameB, PlayerWhite.TimeRemainingMs, PlayerBlack.TimeRemainingMs, isPlaying);
    }

    public void DrawOverlay()
    {
      BotBrainCapacityUI.Draw(tokenCount, debugTokenCount, MaxTokenCount);
      MenuUI.DrawButtons(this);
      MatchStatsUI.DrawMatchStats(this);
    }

    static string GetPlayerName(ChessPlayer player) => GetPlayerName(player.PlayerType);
    static string GetPlayerName(PlayerType type) => type.ToString();

    public void StartNewBotMatch(PlayerType botTypeA, PlayerType botTypeB)
    {
      EndGame(GameResult.DrawByArbiter, log: false, autoStartNextBotMatch: false);
      botMatchGameIndex = 0;
      string nameA = GetPlayerName(botTypeA);
      string nameB = GetPlayerName(botTypeB);
      if (nameA == nameB)
      {
        nameA += " (A)";
        nameB += " (B)";
      }
      BotStatsA = new BotMatchStats(nameA);
      BotStatsB = new BotMatchStats(nameB);
      botAPlaysWhite = true;
      Log($"Starting new match: {nameA} vs {nameB}", false, ConsoleColor.Blue);
      StartNewGame(botTypeA, botTypeB);
    }


    ChessPlayer PlayerToMove => board.IsWhiteToMove ? PlayerWhite : PlayerBlack;
    ChessPlayer PlayerNotOnMove => board.IsWhiteToMove ? PlayerBlack : PlayerWhite;

    public int TotalGameCount => botMatchStartFens.Length * 2;
    public int CurrGameNumber => Math.Min(TotalGameCount, botMatchGameIndex + 1);
    public string AllPGNs => pgns.ToString();


    bool IsLegal(Move givenMove)
    {
      var moves = moveGenerator.GenerateMoves(board);
      foreach (var legalMove in moves)
      {
        if (givenMove.Value == legalMove.Value)
        {
          return true;
        }
      }

      return false;
    }

    public class BotMatchStats
    {
      public string BotName;
      public int NumWins;
      public int NumLosses;
      public int NumDraws;
      public int NumTimeouts;
      public int NumIllegalMoves;

      public BotMatchStats(string name) => BotName = name;
    }

    public void Release()
    {
      boardUI.Release();
    }
  }
}
