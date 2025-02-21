﻿// using System;
// using System.Collections.Generic;
// using System.Linq;
// using ChessChallenge.API;

// public class MyBot : IChessBot
// {

//     public static int POSITIONS_EVALUATED;

//     int calculateWhenToStop(ChessChallenge.API.Timer timer) {
//       return 200 + timer.MillisecondsRemaining / 40;
//     }

//     public static Timer timer;
//     public static int whenToStop;


//         void printNodeMoveRec(Node node, Board board, Move? prefMove) {
//             if(node == null) return;
            
//             if(prefMove != Move.NullMove) board.MakeMove(prefMove.GetValueOrDefault());

//             Console.Write(node.move.toSANString(board.board)+", ");
//             printNodeMoveRec(node.bestNode, board, node.move);
            
//             if(prefMove != Move.NullMove) board.UndoMove(prefMove.GetValueOrDefault());
//         }

//     public Move Think(Board board, Timer timer)
//     {

//         MyBot.timer = timer;
        
//         // Console.WriteLine("STATIC Evaluation (white): " + Node.EvaluatePosition(board, 0));

//         POSITIONS_EVALUATED = 0;

//         PieceList[] pieces = board.GetAllPieceLists();

//         Move bestMove = board.GetLegalMoves()[0];
        
//         LinkedList<Node> nodes = new LinkedList<Node>();
        

//         Node rootNode = new Node(Move.NullMove, null, board.IsWhiteToMove ? 1 : -1, board, "");
        

//         // Bad move on whenToStop: 1000, depth: 9

//         whenToStop = calculateWhenToStop(timer);
//         // whenToStop = 2500;
//         // #if DEBUG
//           //whenToStop = 99999999;
//         // #endif
        
//         Console.WriteLine("whenToStop: " +whenToStop);

//         // rootNode.negaMax(4, board, 0, float.MinValue, float.MaxValue);
//         for(int i = 1; i <= 6; i++) {
//           Console.WriteLine("Calculating depth: " + i);
//           rootNode.negaMax(i, board, 0, float.MinValue, float.MaxValue);
//           Console.Write("Depth " + i+", score: " + -rootNode.bestNode.moveScore+", ");
//           if(timer.MillisecondsElapsedThisTurn >= whenToStop) {
//             break;
//           }
//         }

//         Console.WriteLine();

//         foreach(Node node in rootNode.childNodes) {
//             Console.Write(node.localPositionsEvaluated/1000+"k] move " + node.move.toSANString(board.board) + ", score: " + -node.moveScore+" ::: ");
//             printNodeMoveRec(node, board, null);
//             Console.WriteLine();
//         }
//         Node bestNode = rootNode.bestNode;
//         float bestMoveScore = -bestNode.moveScore;
//         bestMove = bestNode.move;



//         // Node nodeToDisplay = bestNode;



//         Console.WriteLine("Best move has score of: " + bestMoveScore);
//         Console.Write("Moves: ");
//         printNodeMoveRec(bestNode, board, null);

//         Console.WriteLine();

//         Console.WriteLine("That took " + timer.MillisecondsElapsedThisTurn+"ms, positions evaluated: " + POSITIONS_EVALUATED/1000+"k");


//         return bestMove;
//     }

    
//     class Node {
//         public static float[] pieceValues = { 0f, 1.00f, 3.00f, 3.10f, 5.00f, 9.00f, 99f };
//         public float moveScore;
//         public List<Node> childNodes;
    
//     public Node bestNode;
//         public Move move;
//         public readonly Node parent;
//         private readonly int player;
//         public int localPositionsEvaluated = 0;
//         public Boolean didSkip = false;
//         private String moveStr = "";
//         private string movesStr;
//         public Boolean onlyCapturesChilds = false;


//         public Node(Move move, Node? parent, int player, Board board, String allMovesStr) {
//             childNodes = new List<Node>();
            
//             this.move = move;
//             this.parent = parent;
//             this.player = player;
//             moveStr = move.toSANString(board.board);

//             this.movesStr = allMovesStr + (moveStr.Equals("Null") ? "" : moveStr + " ");
//         }

//         // [MethodImpl(MethodImplOptions.AggressiveOptimization)]
//         public void negaMax(int maxDepth, Board board, int currentDepth, float alpha, float beta) {

//             var onlyDoCaptures = currentDepth >= maxDepth && move.IsCapture && currentDepth < maxDepth * 2;

//             if((currentDepth >= maxDepth && !onlyDoCaptures) || board.IsInCheckmate() || board.IsDraw()) {
//                   moveScore = EvaluatePosition(board, currentDepth) * player;
//                   return;
                
//             }
//             if(currentDepth <= 3 && 
//               MyBot.timer.MillisecondsElapsedThisTurn >= MyBot.whenToStop 
//               //MyBot.POSITIONS_EVALUATED >= 148000
//             ) {
//               Console.WriteLine("Stopping!, over time moveScore ::: " + moveScore+" curMove: " + move.ToString());

//               maxDepth = Math.Min(currentDepth + 1, maxDepth);
//               didSkip = true;
//               // return;
//             }
//             if(movesStr.Equals("dxc4 Nxc7+ Kd6 ")) {

//             }

//             moveScore = float.MinValue;

//             if(childNodes.Count == 0) {
//                 System.Span<Move> moves = stackalloc Move[256];
//                 board.GetLegalMovesNonAlloc(ref moves);
                
//                 // Console.WriteLine(moves.Length);
//                 // childNodes = new ArrayList<Move>();

//                 // for (int index = 0; index < moves.Length; index++)
//                 //     childNodes.Add(new Node(moves[index], this, -player));

//                 // If in check, we want to get all moves anyways. Treat a check like a capture
                
//                 childNodes = new List<Move>(board.GetLegalMoves(onlyDoCaptures))// && !board.IsInCheck()))
//                     .ConvertAll<Node>(move => new Node(move, this, -player, board, movesStr));

//                 if(onlyDoCaptures && childNodes.Count == 0) {
//                   moveScore = EvaluatePosition(board, currentDepth) * player;
//                   return;
//                 }
//             } else if(onlyCapturesChilds && !onlyDoCaptures) {
//               // Add normal moves to childNodes
//               childNodes.AddRange(
//                 new List<Move>(board.GetLegalMoves())
//                   .Where(move => !move.IsCapture)
//                   .Select<Move, Node>(move => new Node(move, this, -player, board, movesStr))
//               );
//             }
//             onlyCapturesChilds = onlyDoCaptures;// && !board.IsInCheck();

//             // Console.WriteLine("sorting in depth " + currentDepth);
//             sortMoves(childNodes, board);

//             foreach (Node node in childNodes) {

//                 board.MakeMove(node.move);
//                 node.negaMax(maxDepth, board, currentDepth + 1, -beta, -alpha);
//                 this.localPositionsEvaluated += node.localPositionsEvaluated;
//                 board.UndoMove(node.move);

//                 if(-node.moveScore > moveScore || bestNode == null) {
//                     bestNode = node;
//                 }

//                 moveScore = Math.Max(
//                     -node.moveScore,
//                     moveScore
//                 );
//                 alpha = Math.Max(alpha, moveScore);
//                 if (alpha >= beta) {
//                     return;
//                 }
//             }

//             if(movesStr.Equals("dxc4 Nxc7+ Kd6 ")) {
              
//             }
//         }
        
//         static void sortMoves(List<Node> nodes, Board board) {
//             nodes.Sort(
//                 (move1, move2) => move1.getBestGuessScore(board).CompareTo(move2.getBestGuessScore(board))
//             );
            
//             // // var firstMove = nodes[1].move.ToString();
//             // var tmp = new List<Node>(nodes);
//             // nodes.Sort(
//             //     (move1, move2) => 1//move1.getBestGuessScore(board).CompareTo(move1.getBestGuessScore(board))
//             // );
//             // nodes.Reverse();

//             // Boolean same = true;
//             // for(int i = 0; i < nodes.Count; i++) {
//             //     if(!nodes[i].move.ToString().Equals(tmp[i].move.ToString())) {
//             //         same = false;
                    
//             //     }
//             // }

//             // if(same) {
//             //     Console.WriteLine("Same!!");

//             // } else {
//             //     Console.WriteLine("Not same...");
//             //     Console.Write("Nodes: ");
//             //     for(int i = 0; i < nodes.Count; i++) {
//             //         Console.Write(nodes[i].move.ToString()+", ");
//             //     }
//             //     Console.WriteLine("");
//             //     Console.Write("Temp : ");
//             //     for(int i = 0; i < tmp.Count; i++) {
//             //         Console.Write(tmp[i].move.ToString()+", ");
//             //     }
//             //     Console.WriteLine();
//             // }

//         }

//         public float getBestGuessScore(Board board) {
//             // Console.WriteLine("Guessing move " + move.toSANString(board.board));

//             if(childNodes.Count != 0)
//                 return moveScore;

            
//             if(!move.IsCapture)
//                 return 0;

//             float pieceValue = pieceValues[((int) move.MovePieceType)];
//             float capturePieceValue = pieceValues[(int) move.MovePieceType];

//             float score = 10 * (capturePieceValue - pieceValue);
//             BitboardHelper.GetPawnAttacks(move.TargetSquare, player == 1);
//             score -= 20 *  (board.SquareIsAttackedByOpponent(move.TargetSquare) ? 1 : 0);

//             return score;
//         }

//         public float EvaluatePosition(Board board, int depth) {
//             POSITIONS_EVALUATED++;
//             localPositionsEvaluated++;
//             float score = 0;
//             if (board.IsInCheckmate()) {
              
//                 return float.MaxValue - depth;
//             }
//             if(board.IsDraw()) {
//                 return 0;
//             }
//             PieceList[] pieces = board.GetAllPieceLists();

//             for(int pieceCounter = 1; pieceCounter < pieceValues.Length; pieceCounter++) {
//                 score += pieces[pieceCounter-1].Count * pieceValues[pieceCounter]
//                     - pieces[pieceCounter + 5].Count * pieceValues[pieceCounter];

//             }

//             // 15 moves, 5 captured: 0,3
//              //           score+= 
//              // 0.001f * (board.IsWhiteToMove ? 1 : -1) * board.GetLegalMoves().Length;
//             return score;
//         }

//         public String ToString() {
//           return "["+moveStr + "] " + moveScore;
//         }
//     }
// }