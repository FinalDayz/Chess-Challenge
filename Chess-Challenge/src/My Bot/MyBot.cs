// # define CODE_FOR_TEST
// # define CODE_FOR_DEV
// # define CODE_FOR_DEBUG

/*
REMOVE LOGS::
  (Debug|Console)\.Write
  //$1.Write

BRING BACK LOGS
  //(Debug|Console)\.Write
  $1.Write
*/

using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot {

  public static int TIMEOUT_CHECKS;
  public static int TIMEOUT_CHECKS_BEFORE_TIMEOUT;

  public static Timer timer;
  public static int whenToStop;
  public static int MAX_TIMEOUT_CHECKS = 9999999;

#if CODE_FOR_TEST

  public static MyBot INSTANCE;
  public MyBot() {
    INSTANCE = this;
    TIMEOUT_CHECKS = 0;
    TIMEOUT_CHECKS_BEFORE_TIMEOUT = 999999999;
    MAX_TIMEOUT_CHECKS = 9999999;
    whenToStop = 0;
  }

#endif

  protected int calculateWhenToStop(ChessChallenge.API.Timer timer) {
    return 200 + timer.MillisecondsRemaining / 36;
  }

  void printNodeMoveRec(Node node, Board board, Move? prefMove) {
    if (node == null) return;

    if (prefMove != Move.NullMove) board.MakeMove(prefMove.GetValueOrDefault());

    Console.Write(node.move.toSANString(board.board) + ", ");
    printNodeMoveRec(node._bestNode, board, node.move);

    if (prefMove != Move.NullMove) board.UndoMove(prefMove.GetValueOrDefault());
  }

  public Move Think(Board board, Timer timer) {
    MyBot.timer = timer;
    TIMEOUT_CHECKS = 0;
    Node rootNode = new Node(Move.NullMove, board.IsWhiteToMove ? 1 : -1, board, "", null);

    whenToStop = calculateWhenToStop(timer);
    // whenToStop = 3500;
#if CODE_FOR_DEBUG
    Random rnd = new Random();
    // whenToStop = rnd.Next(1000, 1900);
    // MAX_TIMEOUT_CHECKS = rnd.Next(50000, 190000);
    // whenToStop = rnd.Next(200, 1800);
    // MAX_TIMEOUT_CHECKS = 32530;
    // whenToStop = 99999;
    // whenToStop = (int) Math.Round(whenToStop * 0.6);

    if (MAX_TIMEOUT_CHECKS != 0 && MAX_TIMEOUT_CHECKS != 9999999) {
      whenToStop = 10 * 60 * 1000;
    } else {
      MAX_TIMEOUT_CHECKS = 9999999;
    }
#endif

    Console.WriteLine("NNEvaluator:: " + Node.NNEvaluator(board));

    // whenToStop = 99999999;

    Console.WriteLine("whenToStop: " + whenToStop + " (MAX_TIMEOUT_CHECKS: " + MAX_TIMEOUT_CHECKS + ")");

    // rootNode.negaMax(4, board, 0, float.MinValue, float.MaxValue);
    for (int i = 1; i <= 12; i++) {
      Console.WriteLine("Calculating depth: " + i + " left: " + (MAX_TIMEOUT_CHECKS - TIMEOUT_CHECKS));
      rootNode.negaMax(i, board, 0, float.MinValue, float.MaxValue, true);
      Console.Write("Depth " + i + ", score: " + -rootNode.moveScore + ", ");
#if CODE_FOR_DEBUG
      if (timer.MillisecondsElapsedThisTurn * 1.3 >= whenToStop
       || TIMEOUT_CHECKS * 1.3 >= MAX_TIMEOUT_CHECKS)
#else
      if (timer.MillisecondsElapsedThisTurn * 1.3 >= whenToStop)
#endif
      {
        // if (rootNode.didSkip) {
        //   rootNode.negaMax(i - 1, board, 0, float.MinValue, float.MaxValue, true);
        // }
        break;
      }
      if (rootNode.moveScore > 99990000) {
        break;
      }
    }


    Console.WriteLine();

    foreach (Node node in rootNode.childNodes) {
      double[] pieceValuesNN = { 0.167, 0.333, 0.5, 0.667, 0.833, 1 };
      Console.Write(node.localPositionsEvaluated / 1000 + "k] move " + node.moveStr + ", score: " + -node.moveScore + " ::: ");

      // printNodeMoveRec(node, board, null);
      Console.WriteLine("");
    }

    Move bestMove = rootNode.bestMove;
    float bestMoveScore = rootNode.moveScore;

    Console.WriteLine("Best move has score of: " + bestMoveScore);
    Console.Write("Moves: ");
    printNodeMoveRec(rootNode._bestNode, board, null);

    Console.WriteLine();

    Console.WriteLine("That took " + timer.MillisecondsElapsedThisTurn + "ms, Timeout checks: " + TIMEOUT_CHECKS / 1000 + "k (" + TIMEOUT_CHECKS_BEFORE_TIMEOUT + ")");

    return bestMove;
  }

#if CODE_FOR_TEST
  public virtual bool shouldStop() {
    return false;
  }
#endif

  public class Node {
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
    public Node(Move move, int player, Board board, string allMovesStr, Node parent) {
      childNodes = new List<Node>();
      this.parent = parent;
      this.move = move;
      this.player = player;
#if CODE_FOR_DEV
      moveStr = move.toSANString(board.board);
      movesStr = allMovesStr + (moveStr.Equals("Null") ? "" : moveStr + " ");
#else
      moveStr = "";
      movesStr = "";
#endif
    }
    // forceNotSkip = forceNotSkip || 

    // [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void negaMax(int maxDepth, Board board, int currentDepth, float alpha, float beta, bool allowTimeout) {

      didSkip = false;
      var onlyDoCaptures = currentDepth >= maxDepth && move.IsCapture && currentDepth < maxDepth * 2;

      if (currentDepth >= maxDepth && !onlyDoCaptures || board.IsInCheckmate() || board.IsDraw()) {
        moveScore = EvaluatePosition(board, currentDepth) * player;
        return;
      }

      moveScore = float.MinValue;
      if (onlyDoCaptures) {
        moveScore = EvaluatePosition(board, currentDepth) * player;
        if (moveScore >= beta) return;
        alpha = Math.Max(alpha, moveScore);
      }

      if (childNodes.Count == 0) {
        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves);

        // If in check, we want to get all moves anyways. Treat a check like a capture
        String movesStr = this.movesStr;
        int player = this.player;
        childNodes = new List<Move>(board.GetLegalMoves(onlyDoCaptures))// && !board.IsInCheck()))
            .ConvertAll(move => new Node(move, -player, board, movesStr, this));

        if (onlyDoCaptures && childNodes.Count == 0) {
          moveScore = EvaluatePosition(board, currentDepth) * player;
          return;
        }
      } else if (onlyCapturesChilds && !onlyDoCaptures) {
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

      if (parent == null && currentDepth == 0 && maxDepth == 4) {

      }

      sortMoves(childNodes, board);

      int index = 0;
      foreach (Node node in childNodes) {

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
        )) {
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
                timer.MillisecondsElapsedThisTurn >= whenToStop)) {
        }

        // Special case where the best move is the node that ran out of time
        if (parent == null && node.didSkip && (-node.moveScore > moveScore) && moveScore != float.MinValue) {

          Console.WriteLine("Special case for move " + node.ToString() + " TIME:" + timer.MillisecondsElapsedThisTurn);
          node.negaMax(maxDepth, board, currentDepth + 1, -beta, -alpha, false);
          Console.WriteLine("Done with special case TIME:" + timer.MillisecondsElapsedThisTurn + " Is best move now? " + (-node.moveScore > moveScore));
        }

        board.UndoMove(node.move);

        if (-node.moveScore > moveScore || bestMove.IsNull) {
          bestMove = node.move;
          _bestNode = node;
        }

        moveScore = Math.Max(
            -node.moveScore,
            moveScore
        );
        alpha = Math.Max(alpha, moveScore);
        if (alpha >= beta) {
          aBCutOff = true;
          didSkip = didSkip || _bestNode.didSkip;
          return;
        }
        aBCutOff = false;
        index++;
      }

    }

    static void sortMoves(List<Node> nodes, Board board) {
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
            //     Console.WriteLine("Same!!");

            // } else {
            //     Console.WriteLine("Not same...");
            //     Console.Write("Nodes: ");
            //     for(int i = 0; i < nodes.Count; i++) {
            //         Console.Write(nodes[i].move.ToString()+", ");
            //     }
            //     Console.WriteLine("");
            //     Console.Write("Temp : ");
            //     for(int i = 0; i < tmp.Count; i++) {
            //         Console.Write(tmp[i].move.ToString()+", ");
            //     }
            //     Console.WriteLine();
            // }
      */
    }

    public float getBestGuessScore(Board board) {
      if (parent?._bestNode == this)
        return -99;

      if (childNodes.Count != 0)
        return moveScore;

      if (move.IsCapture) {
        return move.MovePieceType - move.CapturePieceType;
      }

      if (move.IsPromotion) {
        return -20;
      }

      return 5;
    }

    static double[] weights = new double[] {-3.8243027, -0.6259895, -0.37566727, -0.47239956, -0.710612, -0.5758254, -0.49746898, -0.48552105, -0.31978747, -0.20994039, -0.5355769, -0.45424956, -0.42631635, -0.56046224, -0.6795604, -0.63361347, -0.5480735, -0.38738698, -0.5245244, -0.5866182, -0.50744843, -0.4884331, -0.7585054, -0.4243735, -0.31372672, -0.38656205, -0.09686734, -0.39928794, -0.52974004, -0.32022175, -0.3146737, -0.028799485, -0.14767027, -0.07442546, -0.30939698, -0.2095173, -0.35186338, -0.47231293, -0.08722693, -0.1361358, 0.13320306, 0.25615627, -0.024895893, -0.090955496, -0.027596785, -0.04066336, 0.053523757, 0.09815753, 0.36272344, 0.15113983, -0.22285508, -0.069823176, -0.004971496, 0.012613961, 0.058946192, -0.111558296, 0.26935557, -0.006360631, 0.13891532, 0.05356277, -0.0586405, -0.009531236, 0.03171585, 0.21991906, 0.30777818, -0.23724122, 1.8934335, 1.8181429, 1.7498094, 1.2891629, 1.5213896, 1.5305052, 1.4873569, 1.6649748, 1.7719163, 1.6085633, 1.3221598, 1.3577207, 1.3075694, 1.1560344, 1.2549427, 1.6531312, 1.6038474, 1.3130639, 1.249512, 1.2322582, 1.2077931, 1.0964093, 1.1331843, 1.1932015, 1.3302195, 1.3028353, 1.2788702, 1.0867782, 0.9958304, 1.2437435, 1.1874086, 0.94614536, 1.1607695, 1.3847502, 1.2099602, 1.1520349, 1.1244527, 1.2745475, 1.3315276, 1.0963913, 1.4069846, 1.2718042, 1.1241181, 1.173396, 1.2873622, 1.0973805, 1.2091244, 1.064121, 1.3299385, 1.1970932, 1.1828874, 1.2240547, 1.1651374, 1.2055373, 1.1073042, 0.9624204, 0.98904115, 1.1299537, 1.0892638, 0.92628735, 0.93325883, 1.0078661, 0.6929659, 0.81907094, -1.0860262, -1.5291945, -1.5446526, -1.6386596, -1.8551036, -1.7345957, -1.7085259, -2.0434437, -1.7073328, -1.4838634, -1.7844384, -1.8936197, -1.7086681, -1.8077457, -1.9397616, -2.0209742, -1.7865292, -1.49778, -1.7573643, -1.9534856, -1.9061402, -1.8453404, -1.9846243, -1.9355063, -1.8656915, -1.7868335, -1.7992986, -1.9169385, -2.087083, -2.1349282, -1.9009427, -1.8497156, -2.0513508, -1.7891518, -1.7330034, -1.8609025, -2.0898051, -2.0801764, -1.8601754, -1.8775036, -1.9966334, -1.5667571, -1.8691453, -2.0709937, -1.9627656, -1.9729813, -2.1796844, -1.9631996, -1.8474096, -1.5257242, -1.8404092, -1.8675066, -1.8582761, -1.8634871, -1.7990979, -2.0603857, -1.7787601, -1.5466118, -1.5850449, -1.7040484, -1.9936635, -1.7697865, -1.7217851, -1.9216329, -1.5951719, -1.1914244, 2.666432, 2.7344306, 2.7740278, 2.9795916, 2.938009, 2.8177595, 3.115569, 2.857758, 2.5305629, 2.853426, 2.9638853, 2.821899, 2.951076, 3.0398848, 3.0847297, 2.8869607, 2.4795008, 2.8822198, 3.1135085, 3.0062573, 2.9625614, 3.1905782, 3.0466263, 2.8534997, 2.8403845, 2.8381302, 2.984906, 3.3043396, 3.2994637, 2.9622335, 2.854664, 3.0169375, 2.857739, 2.8350778, 2.9287636, 3.3008647, 3.2240858, 2.8612635, 2.9462435, 3.0213404, 2.5472572, 2.9354024, 3.2345693, 3.0728061, 3.0297847, 3.272788, 3.0252428, 2.8201957, 2.634069, 2.7969642, 2.9495656, 2.9289868, 2.9926245, 3.008207, 3.1194675, 2.8373182, 2.6750414, 2.7424686, 2.803549, 3.071774, 2.878654, 2.8415108, 2.9660394, 2.731547, 0.2011749, -0.5977932, -0.93059766, -0.86846846, -0.6380636, -0.64032054, -0.72724533, -0.5768038, -0.58396184, -1.0036402, -0.73376966, -0.89185554, -0.9268876, -0.8643057, -0.7554052, -0.7124735, -0.6994245, -1.0039701, -0.98224324, -0.81590515, -0.8991381, -0.9611139, -0.8169027, -0.9189856, -0.8771333, -0.88603586, -1.0237863, -0.90050524, -0.851748, -0.8120245, -0.9936836, -1.0776674, -0.8890886, -0.94460684, -0.98616403, -1.039382, -0.78711575, -0.84395194, -1.0271326, -0.9244701, -0.8803887, -1.1261092, -0.91802746, -0.77769256, -0.84677136, -0.8749024, -0.73675853, -0.8108007, -1.0569977, -1.2151271, -0.9400552, -0.94610137, -0.92844427, -0.85802037, -0.8985812, -0.7876744, -1.2209829, -1.0003076, -1.2001222, -1.0932157, -0.7619149, -1.0721552, -0.97180927, -1.1845021, -1.3053963, -0.40199274, 0.32173216, -0.5213129, 0.67815465, -0.45913598
    };
    static double[] bias = new double[] {
      0.0, 0.0, 0.0, 0.0, 0.0, 0.0
    };
    static double[] pieceValuesNN = { 0.167, 0.333, 0.5, 0.667, 0.833, 1 };

    public static double NNEvaluator(Board board) {

      double[] input = new double[65];
      input[0] = board.IsWhiteToMove ? 1 : -1;
      for (int sideToMove = -1; sideToMove <= 1; sideToMove += 2)
        for (int pieceType = 1; pieceType < 7; pieceType++)
          for (ulong mask = board.GetPieceBitboard((PieceType)pieceType, sideToMove > 0); mask != 0;) {

            int square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56;
            // Console.WriteLine("Piece " + pieceType + " sideToMove: " + sideToMove + " square + 1: " + (square + 1) + " pieceValue: " + (pieceValues[pieceType - 1] * sideToMove));
            input[square + 1] = pieceValuesNN[pieceType - 1] * sideToMove;
          }

      // Console.WriteLine(String.Join("|", input));

      // 0,6667|0|0|0|0|0|0|0,6667|
      // 0|0,1667|0|1|0|0,1667|0,1667|0|
      // 0|0,8333|0,1667|0,1667|0,5|0,3333|0,3333|0|
      // 0|0|0|0|0,1667|0|0|0,1667|
      // 0,1667|0|0|0|-0,1667|0|0|0|
      // -0,1667|0|-0,3333|-0,1667|0,5|-0,3333|0|-0,1667|
      // 0|-0,1667|-0,1667|0|0|0|-0,1667|-1|
      // -0,6667 | 0 | -0,5 | -0,8333 | 0 | -0,5 | 0 | 0


      var layerSize = new[] { 65, 5, 1 };
      var layerOutputs = new[] { input, new double[20], new double[1] };

      int weightIndex = 0;
      int biasIndex = 0;
      for (int outputLayer = 1; outputLayer < layerOutputs.Length; outputLayer++) {
        for (int outputLayerY = 0; outputLayerY < layerSize[outputLayer]; outputLayerY++) {
          for (int inputLayerY = 0; inputLayerY < layerSize[outputLayer - 1]; inputLayerY++) {

            layerOutputs[outputLayer][outputLayerY] += layerOutputs[outputLayer - 1][inputLayerY] * weights[weightIndex];
            weightIndex++;
          }
          layerOutputs[outputLayer][outputLayerY] = Math.Tanh(layerOutputs[outputLayer][outputLayerY] + bias[biasIndex]);
          biasIndex++;
        }
      }

      return layerOutputs[layerOutputs.Length - 1][0];
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
    public float EvaluatePosition(Board board, int depth) {
      // TIMEOUT_CHECKS++;
      localPositionsEvaluated++;
      int player = board.IsWhiteToMove ? 1 : -1;


      if (board.IsInCheckmate()) {
        return -player * (99999999 - depth);
      }

      if (board.IsDraw()) {
        return 0;
      }

      return (float)NNEvaluator(board);

      // float score = 0;
      // PieceList[] pieces = board.GetAllPieceLists();

      // for (int pieceCounter = 1; pieceCounter < pieceValues.Length; pieceCounter++) {
      //   score += pieces[pieceCounter - 1].Count * pieceValues[pieceCounter]
      //       - pieces[pieceCounter + 5].Count * pieceValues[pieceCounter];
      // }

      // if (board.IsInCheck()) {
      //   return score;
      // }


      // Span<Move> moves = stackalloc Move[100];
      // board.GetLegalMovesNonAlloc(ref moves);

      // score += 0.001f * player * evaluateAvailableMoves(moves);

      // if (board.TrySkipTurn()) {
      //   Span<Move> movesOtherPlayer = stackalloc Move[100];
      //   board.GetLegalMovesNonAlloc(ref movesOtherPlayer);
      //   score += 0.001f * -player * evaluateAvailableMoves(movesOtherPlayer);

      //   board.UndoSkipTurn();
      // }

      // return score;// + (float)NNEvaluator(board);
    }

    private static readonly float[] movePieceTypePoints = { 0f, 1f, 3f, 3f, 3f, 3f, 1f };
    private float evaluateAvailableMoves(Span<Move> moves) {
      return moves.Length;
      // float score = 0;
      // foreach (Move move in moves) {
      //   score += movePieceTypePoints[(int)move.MovePieceType];
      // }
      // return score;
    }

    public string ToString() {
      return "[" + moveStr + "] for " + (player == -1 ? "black" : "white") + ": " + moveScore;
    }
  }
}
