// #define DEV

using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
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


    Node rootNode = new Node(Move.NullMove, board.IsWhiteToMove ? 1 : -1, board, "", null);


    // Bad move on whenToStop: 1000, depth: 9

    whenToStop = calculateWhenToStop(timer);
    // whenToStop = 100;
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
#if DEV
      // if (timer.MillisecondsElapsedThisTurn >= whenToStop)break;
#else
        if (timer.MillisecondsElapsedThisTurn >= whenToStop)break;
#endif
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
    private Boolean ABCutOff = false;


    public Node(Move move, int player, Board board, string allMovesStr, Node parent)
    {
      childNodes = new List<Node>();
      this.parent = parent;
      this.move = move;
      this.player = player;
#if DEV
      moveStr = move.toSANString(board.board);
      movesStr = allMovesStr + (moveStr.Equals("Null") ? "" : moveStr + " ");
#else
            moveStr = "";
            movesStr = "";
#endif
    }

    // [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void negaMax(int maxDepth, Board board, int currentDepth, float alpha, float beta)
    {

      if (movesStr.Equals("Qxf5 ")
        && POSITIONS_EVALUATED >= 17000
       )
      {

      }

      var onlyDoCaptures = currentDepth >= maxDepth && move.IsCapture && currentDepth < maxDepth * 2;

      if (currentDepth >= maxDepth && !onlyDoCaptures || board.IsInCheckmate() || board.IsDraw())
      {
        moveScore = EvaluatePosition(board, currentDepth) * player;
        return;
      }
      if (currentDepth <= 3 &&
#if DEV
      // POSITIONS_EVALUATED >= 17000
      MyBot.timer.MillisecondsElapsedThisTurn >= MyBot.whenToStop
#else
          MyBot.timer.MillisecondsElapsedThisTurn >= MyBot.whenToStop
#endif

     )
      {
        if (!ABCutOff)
        {
          // maxDepth = Math.Min(currentDepth + 1, maxDepth);
          didSkip = true;
          return;
        }
      }


      moveScore = /*onlyDoCaptures ? EvaluatePosition(board, currentDepth) * player : */float.MinValue;

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
          ABCutOff = true;
          return;
        }
        ABCutOff = false;
      }

      if (movesStr.Equals("Rc4 "))
      {

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
      POSITIONS_EVALUATED++;
      localPositionsEvaluated++;
      // return Evaluate(board);
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
      return "[" + moveStr + "] " + moveScore;
    }
  }
}
