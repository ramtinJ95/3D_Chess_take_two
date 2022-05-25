using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        int direction = (team == 0) ? 1 : -1;

        if (board[currentX, currentY + direction] == null)
        {
            // white start move
            if(team == 0 && currentY == 1 && board[currentX, currentY + (direction * 2)]==null)
            {
                r.Add(new Vector2Int(currentX, currentY + direction));
                r.Add(new Vector2Int(currentX, currentY + (direction * 2) ));
            }
            // black start move
            else if (team == 1 && currentY == 6 && board[currentX, currentY + (direction * 2)]==null)
            {
                r.Add(new Vector2Int(currentX, currentY + direction));
                r.Add(new Vector2Int(currentX, currentY + (direction * 2)));
            }
            else
            {
                r.Add(new Vector2Int(currentX, currentY + direction));
            }
        }



        return r;
    }
}
