using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ChessChallenge.API;

namespace ChessChallenge.Example
{
  /*
  REMOVE LOGS::
    (Debug|Console)\.Write
    //$1.Write

  BRING BACK LOGS
    //(Debug|Console)\.Write
    $1.Write
  */

  public class EvilBot : IChessBot
  {

    public static int TIMEOUT_CHECKS;
    public static int TIMEOUT_CHECKS_BEFORE_TIMEOUT;

    public static Timer timer;
    public static int whenToStop;
    public static int MAX_TIMEOUT_CHECKS = 9999999;


    protected int calculateWhenToStop(ChessChallenge.API.Timer timer)
    {
      return 200 + timer.MillisecondsRemaining / 36;
    }

    void printNodeMoveRec(Node node, Board board, Move? prefMove)
    {
      if (node == null) return;

      if (prefMove != Move.NullMove) board.MakeMove(prefMove.GetValueOrDefault());

      //Console.Write(node.move.toSANString(board.board) + ", ");
      printNodeMoveRec(node._bestNode, board, node.move);

      if (prefMove != Move.NullMove) board.UndoMove(prefMove.GetValueOrDefault());
    }

    public Move Think(Board board, Timer timer)
    {
      EvilBot.timer = timer;
      TIMEOUT_CHECKS = 0;
      Node rootNode = new Node(Move.NullMove, board.IsWhiteToMove ? 1 : -1, board, "", null);

      whenToStop = calculateWhenToStop(timer);


      // whenToStop = 99999999;

      //Console.WriteLine("whenToStop: " + whenToStop + " (MAX_TIMEOUT_CHECKS: " + MAX_TIMEOUT_CHECKS + ")");

      // rootNode.negaMax(4, board, 0, float.MinValue, float.MaxValue);
      for (int i = 1; i <= 12; i++)
      {
        //Console.WriteLine("Calculating depth: " + i + " left: " + (MAX_TIMEOUT_CHECKS - TIMEOUT_CHECKS));
        rootNode.negaMax(i, board, 0, float.MinValue, float.MaxValue, true);
        //Console.Write("Depth " + i + ", score: " + -rootNode.moveScore + ", ");

        if (timer.MillisecondsElapsedThisTurn * 1.3 >= whenToStop)

        {
          // if (rootNode.didSkip) {
          //   rootNode.negaMax(i - 1, board, 0, float.MinValue, float.MaxValue, true);
          // }
          break;
        }
        if (rootNode.moveScore > 99990000)
        {
          break;
        }
      }


      //Console.WriteLine();

      foreach (Node node in rootNode.childNodes)
      {
        //Debug.Write(node.localPositionsEvaluated / 1000 + "k] move " + node.moveStr + ", score: " + -node.moveScore + " ::: ");
        // printNodeMoveRec(node, board, null);
        //Debug.WriteLine("");
      }

      Move bestMove = rootNode.bestMove;
      float bestMoveScore = rootNode.moveScore;

      //Console.WriteLine("Best move has score of: " + bestMoveScore);
      //Console.Write("Moves: ");
      printNodeMoveRec(rootNode._bestNode, board, null);

      //Console.WriteLine();

      //Console.WriteLine("That took " + timer.MillisecondsElapsedThisTurn + "ms, Timeout checks: " + TIMEOUT_CHECKS / 1000 + "k (" + TIMEOUT_CHECKS_BEFORE_TIMEOUT + ")");

      return bestMove;
    }


    public class Node
    {
      public static float[] pieceValues = { 0f, 1.00f, 3.00f, 3.10f, 5.00f, 9.00f, 99f };
      public float moveScore = 0;
      public List<Node> childNodes;
      public Node _bestNode;

      public Move bestMove = Move.NullMove;
      public Move move;
      private int player;
      public int localPositionsEvaluated = 0;
      public bool didSkip = false;
      public readonly string moveStr = "";
      public readonly string movesStr;
      public bool onlyCapturesChilds = false;
      public Node parent;
      private Boolean aBCutOff = false;

      /*

      The Move object is the move that has been made to reach this node. So this node is AFTER the move.
      The score is then relative to the player whose turn it is.
      So a score of -1000 is bad for the current player (1 white, -1 black)

      */
      public Node(Move move, int player, Board board, string allMovesStr, Node parent)
      {
        childNodes = new List<Node>();
        this.parent = parent;
        this.move = move;
        this.player = player;

        moveStr = "";
        movesStr = "";

      }
      // forceNotSkip = forceNotSkip || 

      // [MethodImpl(MethodImplOptions.AggressiveOptimization)]
      public void negaMax(int maxDepth, Board board, int currentDepth, float alpha, float beta, bool allowTimeout)
      {

        didSkip = false;
        var onlyDoCaptures = currentDepth >= maxDepth && move.IsCapture && currentDepth < maxDepth * 2;

        if (currentDepth >= maxDepth && !onlyDoCaptures || board.IsInCheckmate() || board.IsDraw())
        {
          moveScore = EvaluatePosition(board, currentDepth) * player;
          return;
        }

        moveScore = float.MinValue;
        if (onlyDoCaptures)
        {
          moveScore = EvaluatePosition(board, currentDepth) * player;
          if (moveScore >= beta) return;
          alpha = Math.Max(alpha, moveScore);
        }

        if (childNodes.Count == 0)
        {
          Span<Move> moves = stackalloc Move[256];
          board.GetLegalMovesNonAlloc(ref moves);

          // If in check, we want to get all moves anyways. Treat a check like a capture
          String movesStr = this.movesStr;
          int player = this.player;
          childNodes = new List<Move>(board.GetLegalMoves(onlyDoCaptures))// && !board.IsInCheck()))
              .ConvertAll(move => new Node(move, -player, board, movesStr, this));

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
              .Select(move => new Node(move, -player, board, movesStr, this))
          );
        }
        onlyCapturesChilds = onlyDoCaptures;// && !board.IsInCheck();

        if (parent == null && currentDepth == 0 && maxDepth == 4)
        {

        }

        sortMoves(childNodes, board);

        int index = 0;
        foreach (Node node in childNodes)
        {

          if (allowTimeout && (
#if CODE_FOR_TEST
        INSTANCE.shouldStop() ||
#endif
#if CODE_FOR_DEBUG
        TIMEOUT_CHECKS >= MAX_TIMEOUT_CHECKS ||
        timer.MillisecondsElapsedThisTurn >= whenToStop
#else
              timer.MillisecondsElapsedThisTurn >= whenToStop
#endif
          ))
          {
            TIMEOUT_CHECKS_BEFORE_TIMEOUT = Math.Min(TIMEOUT_CHECKS_BEFORE_TIMEOUT, TIMEOUT_CHECKS - 1);
            // moveScore = -99999;
            didSkip = true;
            return;
          }
          TIMEOUT_CHECKS++;

          board.MakeMove(node.move);
          node.negaMax(maxDepth, board, currentDepth + 1, -beta, -alpha, allowTimeout);
          localPositionsEvaluated += node.localPositionsEvaluated;

          if (parent == null && (TIMEOUT_CHECKS >= MAX_TIMEOUT_CHECKS ||
                  timer.MillisecondsElapsedThisTurn >= whenToStop))
          {
          }

          // Special case where the best move is the node that ran out of time
          if (parent == null && node.didSkip && (-node.moveScore > moveScore) && moveScore != float.MinValue)
          {

            //Console.WriteLine("Special case for move " + node.ToString() + " TIME:" + timer.MillisecondsElapsedThisTurn);
            node.negaMax(maxDepth, board, currentDepth + 1, -beta, -alpha, false);
            //Console.WriteLine("Done with special case TIME:" + timer.MillisecondsElapsedThisTurn + " Is best move now? " + (-node.moveScore > moveScore));
          }

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
            aBCutOff = true;
            didSkip = didSkip || _bestNode.didSkip;
            return;
          }
          aBCutOff = false;
          index++;
        }

      }

      static void sortMoves(List<Node> nodes, Board board)
      {
        nodes.Sort(
            (move1, move2) => move1.getBestGuessScore(board).CompareTo(move2.getBestGuessScore(board))
        );
        /*
              // // var firstMove = nodes[1].move.ToString();
              // var tmp = new List<Node>(nodes);
              // nodes.Sort(
              //     (move1, move2) => 1//move1.getBestGuessScore(board).CompareTo(move1.getBestGuessScore(board))
              // );
              // nodes.Reverse();

              // Boolean same = true;
              // for(int i = 0; i < nodes.Count; i++) {
              //     if(!nodes[i].move.ToString().Equals(tmp[i].move.ToString())) {
              //         same = false;

              //     }
              // }

              // if(same) {
              //     //Console.WriteLine("Same!!");

              // } else {
              //     //Console.WriteLine("Not same...");
              //     //Console.Write("Nodes: ");
              //     for(int i = 0; i < nodes.Count; i++) {
              //         //Console.Write(nodes[i].move.ToString()+", ");
              //     }
              //     //Console.WriteLine("");
              //     //Console.Write("Temp : ");
              //     for(int i = 0; i < tmp.Count; i++) {
              //         //Console.Write(tmp[i].move.ToString()+", ");
              //     }
              //     //Console.WriteLine();
              // }
        */
      }

      public float getBestGuessScore(Board board)
      {
        if (parent?._bestNode == this)
          return -99;

        if (childNodes.Count != 0)
          return moveScore;

        if (move.IsCapture)
        {
          return move.MovePieceType - move.CapturePieceType;
        }

        if (move.IsPromotion)
        {
          return 20;
        }

        return 5;
      }

      /*
          static int[] pieceVal = {0, 100, 310, 330, 500, 1000, 10000 };
          static int[] piecePhase = {0, 0, 1, 1, 2, 4, 0};
          static ulong[] psts = {657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047, 347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596, 67159620133902};

          static int getPstVal(int psq) {
              return (int)(((psts[psq / 10] >> (6 * (psq % 10))) & 63) - 20) * 8;
          }

          public int Evaluate(Board board) {
              int mg = 0, eg = 0, phase = 0;

              foreach(bool stm in new[] {true, false}) {
                  for(var p = PieceType.Pawn; p <= PieceType.King; p++) {
                      int piece = (int)p, ind;
                      ulong mask = board.GetPieceBitboard(p, stm);
                      while(mask != 0) {
                          phase += piecePhase[piece];
                          ind = 128 * (piece - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ (stm ? 56 : 0);
                          mg += getPstVal(ind) + pieceVal[piece];
                          eg += getPstVal(ind + 64) + pieceVal[piece];
                      }
                  }

                  mg = -mg;
                  eg = -eg;
              }

              return (mg * phase + eg * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : 1);
          }
      */
      public float EvaluatePosition(Board board, int depth)
      {
        // TIMEOUT_CHECKS++;
        localPositionsEvaluated++;
        // return Evaluate(board);
        int player = board.IsWhiteToMove ? 1 : -1;

        float score = 0;
        if (board.IsInCheckmate())
        {
          return -player * (99999999 - depth);
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

        if (board.IsInCheck())
        {
          return score;
        }


        Span<Move> moves = stackalloc Move[100];
        board.GetLegalMovesNonAlloc(ref moves);

        score += 0.001f * player * moves.Length;

        if (board.TrySkipTurn())
        {
          Span<Move> movesOtherPlayer = stackalloc Move[100];
          board.GetLegalMovesNonAlloc(ref movesOtherPlayer);
          score += 0.001f * -player * movesOtherPlayer.Length;

          board.UndoSkipTurn();
        }

        return score;
      }

      public string ToString()
      {
        return "[" + moveStr + "] for " + (player == -1 ? "black" : "white") + ": " + moveScore;
      }
    }
  }

}