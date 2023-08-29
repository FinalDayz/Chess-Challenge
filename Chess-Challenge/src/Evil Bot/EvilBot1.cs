using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class EvilBot : IChessBot
    {

  public static int POSITIONS_EVALUATED;

  int calculateWhenToStop(ChessChallenge.API.Timer timer)
  {
    return 200 + timer.MillisecondsRemaining / 40;
  }

  public static Timer timer;
  public static int whenToStop;


  // void printNodeMoveRec(Node node, Board board, Move? prefMove)
  // {
  //   if (node == null) return;

  //   if (prefMove != Move.NullMove) board.MakeMove(prefMove.GetValueOrDefault());

  //   //Console.Write(node.move.toSANString(board.board) + ", ");
  //   printNodeMoveRec(node.bestNode, board, node.move);

  //   if (prefMove != Move.NullMove) board.UndoMove(prefMove.GetValueOrDefault());
  // }

  public Move Think(Board board, Timer timer)
  {

    MyBot.timer = timer;

    // //Console.WriteLine("STATIC Evaluation (white): " + Node.EvaluatePosition(board, 0));

    POSITIONS_EVALUATED = 0;

    PieceList[] pieces = board.GetAllPieceLists();

    Move bestMove = board.GetLegalMoves()[0];
    //Console.WriteLine("First move: " + bestMove);

    LinkedList<Node> nodes = new LinkedList<Node>();


    Node rootNode = new Node(Move.NullMove, board.IsWhiteToMove ? 1 : -1, board, "");


    // Bad move on whenToStop: 1000, depth: 9

    whenToStop = calculateWhenToStop(timer);
    // whenToStop = 2500;
    // #if DEBUG
    // whenToStop = 99999999;
    // #endif

    //Console.WriteLine("whenToStop: " + whenToStop);

    // rootNode.negaMax(4, board, 0, float.MinValue, float.MaxValue);
    for (int i = 1; i <= 10; i++)
    {
      //Console.WriteLine("Calculating depth: " + i);
      rootNode.negaMax(i, board, 0, float.MinValue, float.MaxValue);
      //Console.Write("Depth " + i + ", score: " + -rootNode.moveScore + ", ");
      if (timer.MillisecondsElapsedThisTurn >= whenToStop)
      {
        break;
      }
    }

    //Console.WriteLine();

    foreach (Node node in rootNode.childNodes)
    {
      //Console.Write(node.localPositionsEvaluated / 1000 + "k] move " + node.moveStr + ", score: " + -node.moveScore + " ::: ");
      // printNodeMoveRec(node, board, null);
      //Console.WriteLine();
    }

    bestMove = rootNode.bestMove;
    float bestMoveScore = rootNode.moveScore;



    // Node nodeToDisplay = bestNode;



    //Console.WriteLine("Best move has score of: " + bestMoveScore);
    //Console.Write("Moves: ");
    // printNodeMoveRec(bestNode, board, null);

    //Console.WriteLine();

    //Console.WriteLine("That took " + timer.MillisecondsElapsedThisTurn + "ms, positions evaluated: " + POSITIONS_EVALUATED / 1000 + "k");

    Console.WriteLine(rootNode._bestNode.ToString());

    return bestMove;
  }


  public class Node
  {
    public static float[] pieceValues = { 0f, 1.00f, 3.00f, 3.10f, 5.00f, 9.00f, 99f };
    public float moveScore = 0;
    public List<Node> childNodes;

    public Move bestMove = Move.NullMove;
    public Move move;
    private int player;
    public int localPositionsEvaluated = 0;
    public bool didSkip = false;
    public readonly string moveStr = "";
    public readonly string movesStr;
    public bool onlyCapturesChilds = false;

    public Node _bestNode;

    public Node(Move move, int player, Board board, string allMovesStr)
    {
      childNodes = new List<Node>();

      this.move = move;
      this.player = player;
      moveStr = move.toSANString(board.board);

      movesStr = allMovesStr + (moveStr.Equals("Null") ? "" : moveStr + " ");
    }

    // [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void negaMax(int maxDepth, Board board, int currentDepth, float alpha, float beta)
    {
      var onlyDoCaptures = currentDepth >= maxDepth && move.IsCapture && currentDepth < maxDepth * 2;

      if (currentDepth >= maxDepth && !onlyDoCaptures || board.IsInCheckmate() || board.IsDraw())
      {
        moveScore = EvaluatePosition(board, currentDepth) * player;
        return;
      }
      if (currentDepth <= 3 &&
        MyBot.timer.MillisecondsElapsedThisTurn >= MyBot.whenToStop 
        // POSITIONS_EVALUATED >= 148000
      )
      {

        maxDepth = Math.Min(currentDepth + 1, maxDepth);
        didSkip = true;
        // return;
      }
      if (movesStr.Equals("dxc4 Nxc7+ Kd6 "))
      {

      }

      moveScore = float.MinValue;

      if (childNodes.Count == 0)
      {
        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves);

        // //Console.WriteLine(moves.Length);
        // childNodes = new ArrayList<Move>();

        // for (int index = 0; index < moves.Length; index++)
        //     childNodes.Add(new Node(moves[index], this, -player));

        // If in check, we want to get all moves anyways. Treat a check like a capture
        String movesStr = this.movesStr;
        int player = this.player;
        childNodes = new List<Move>(board.GetLegalMoves(onlyDoCaptures))// && !board.IsInCheck()))
            .ConvertAll(move => new Node(move, -player, board, movesStr));

        if (onlyDoCaptures && childNodes.Count == 0)
        {
          moveScore = EvaluatePosition(board, currentDepth) * player;
          return;
        }
      }
      else if (onlyCapturesChilds && !onlyDoCaptures)
      {
                  String movesStr = this.movesStr;
        int player = this.player;
        // Add normal moves to childNodes
        childNodes.AddRange(
          new List<Move>(board.GetLegalMoves())
            .Where(move => !move.IsCapture)
            .Select(move => new Node(move, -player, board, movesStr))
        );
      }
      onlyCapturesChilds = onlyDoCaptures;// && !board.IsInCheck();

      // //Console.WriteLine("sorting in depth " + currentDepth);
      sortMoves(childNodes, board);

      foreach (Node node in childNodes)
      {

        board.MakeMove(node.move);
        node.negaMax(maxDepth, board, currentDepth + 1, -beta, -alpha);
        localPositionsEvaluated += node.localPositionsEvaluated;
        board.UndoMove(node.move);

        if (-node.moveScore > moveScore || bestMove.IsNull)
        {
          bestMove = node.move;
          _bestNode = node;
        }

        moveScore = Math.Max(
            -node.moveScore,
            moveScore
        );
        alpha = Math.Max(alpha, moveScore);
        if (alpha >= beta)
        {
          return;
        }
      }
    }

    static void sortMoves(List<Node> nodes, Board board)
    {
      nodes.Sort(
          (move1, move2) => move1.getBestGuessScore(board).CompareTo(move2.getBestGuessScore(board))
      );

    }

    public float getBestGuessScore(Board board)
    {
      // //Console.WriteLine("Guessing move " + move.toSANString(board.board));

      if (childNodes.Count != 0)
        return moveScore;


      if (!move.IsCapture)
        return 0;

      float pieceValue = pieceValues[(int)move.MovePieceType];
      float capturePieceValue = pieceValues[(int)move.MovePieceType];

      float score = 10 * (capturePieceValue - pieceValue);
      BitboardHelper.GetPawnAttacks(move.TargetSquare, player == 1);
      score -= 20 * (board.SquareIsAttackedByOpponent(move.TargetSquare) ? 1 : 0);

      return score;
    }

    public float EvaluatePosition(Board board, int depth)
    {
      POSITIONS_EVALUATED++;
      localPositionsEvaluated++;
      int player = board.IsWhiteToMove ? 1 : -1;

      float score = 0;
      if (board.IsInCheckmate())
      {
        return -player * (999999 - depth);
      }
      
      if (board.IsDraw())
      {
        return 0;
      }
      PieceList[] pieces = board.GetAllPieceLists();

      for (int pieceCounter = 1; pieceCounter < pieceValues.Length; pieceCounter++)
      {
        score += pieces[pieceCounter - 1].Count * pieceValues[pieceCounter]
            - pieces[pieceCounter + 5].Count * pieceValues[pieceCounter];
      }

      // if(board.IsInCheck()) {
      //   return score;
      // }

      score += 0.001f * player * board.GetLegalMoves().Length;

      // if(board.TrySkipTurn()) {
      //   score += 0.001f * -player * board.GetLegalMoves().Length;

      //   board.UndoSkipTurn();
      // }
      

      return score;
    }

    public string ToString()
    {
      return "[" + moveStr + "] " + moveScore;
    }
  }
    }
}