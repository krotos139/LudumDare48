using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WaterGrid : MonoBehaviour
{
    public enum cellType
    {
        empty,
        ground,
        water
    };

    int[] shiftX = new int[8] { -1, 0, 1, 1,  1,  0, -1, -1 };
    int[] shiftY = new int[8] {  1, 1, 1, 0, -1, -1, -1,  0 };

    uint width, height;

    int[,] connectivity;

    public cellType[,] cells;

    public int vyazkost = 3;

    public class Coords
    {
        public int x, y;
    }
    Dictionary<int, Coords> emptyNeighbour;
    Dictionary<int, Coords> filledTop;

    T[,] build2dArray<T>(uint width, uint height, T def)
    {
        var array = new T[width, height];
        for (uint i = 0; i < width; ++i)
            for (uint j = 0; j < height; ++j)
                array[i, j] = def;

        return array;
    }

    public WaterGrid(uint width, uint height)
    {

        this.width = width;
        this.height = height;

        cells = build2dArray<cellType>(width, height, cellType.empty);
        connectivity = build2dArray<int>(width, height, -1);

        emptyNeighbour = new Dictionary<int, Coords>();
        filledTop = new Dictionary<int, Coords>();
    }

    void resetConnectivityData()
    {
        emptyNeighbour.Clear();
        filledTop.Clear();

        for (uint i = 0; i < width; ++i)
            for (uint j = 0; j < height; ++j)
                connectivity[i, j] = -1;
    }

    public int findLakes()
    {
        resetConnectivityData();

        int lakeIndex = 0;
        for (int i = 0; i < width; ++i)
        {
            for (int j = 0; j < height; ++j)
            {
                if(cells[i, j] == cellType.water && connectivity[i, j] == -1)
                {
                    markLake(i, j, lakeIndex, 0);
                    lakeIndex++;
                }
            }
        }

        return lakeIndex;
    }


    void updateNeighbour(int i, int j, int lakeIndex)
    {
        if (i >= 0 && j >= 0 && i < width && j < height && cells[i, j] == cellType.empty)
        {
            Coords bottomNeighbour;
            if (emptyNeighbour.TryGetValue(lakeIndex, out bottomNeighbour))
            {
                if (bottomNeighbour.y <= j)
                {
                    if (bottomNeighbour.y == j)
                    {
                        if(Random.Range(0, 4) == 2)
                        {
                            emptyNeighbour[lakeIndex] = new Coords() { x = i, y = j };
                        }
                    }
                    else
                    {
                        emptyNeighbour[lakeIndex] = new Coords() { x = i, y = j };
                    }
                }
            }
            else
            {
                emptyNeighbour[lakeIndex] = new Coords() { x = i, y = j};
            }
        }
    }

    void markLake(int startI, int startJ, int lakeIndex, int deep)
    {
        Queue<Coords> q = new Queue<Coords>();
        
        q.Enqueue(new Coords() { x = startI, y = startJ });
        connectivity[startI, startJ] = lakeIndex;

        while (q.Count != 0)
        {
            Coords cur = q.Dequeue();
            int i = cur.x;
            int j = cur.y;

            //filled top
            Coords prevTop;
            if (filledTop.TryGetValue(lakeIndex, out prevTop))
            {
                if (prevTop.y > j)
                {
                    filledTop[lakeIndex] = new Coords() { x = i, y = j };
                }
            }
            else
            {
                filledTop[lakeIndex] = new Coords() { x = i, y = j };
            }

            // tops
            updateNeighbour(i, j - 1, lakeIndex);
            // empty bottom
            updateNeighbour(i, j + 1, lakeIndex);
            // right side
            if (Random.Range(0, vyazkost) < 5) updateNeighbour(i + 1, j, lakeIndex);
            // left side
            if (Random.Range(0, vyazkost) < 5) updateNeighbour(i - 1, j, lakeIndex);

            for (int moveIndex = 0; moveIndex < 8; ++moveIndex)
            {
                var nextI = i + shiftX[moveIndex];
                var nextJ = j + shiftY[moveIndex];

                if (nextI >= 0 && nextI < width && nextJ >= 0 && nextJ < height)
                {
                    if (cells[nextI, nextJ] == cellType.water && connectivity[nextI, nextJ] == -1)
                    {
                        connectivity[nextI, nextJ] = lakeIndex;
                        q.Enqueue(new Coords() { x = nextI, y = nextJ });
                    }
                }
            }
        }
    }

    public void nextStep()
    {
        int lakesCount = findLakes();

       // comminucating vessels part
        for (int lakeIndex = 0; lakeIndex < lakesCount; ++lakeIndex)
        {
            var cellToMove = filledTop[lakeIndex];

            Coords emptyPlace;

            if (emptyNeighbour.TryGetValue(lakeIndex, out emptyPlace)) {
                if (cells[emptyPlace.x, emptyPlace.y] == cellType.empty)
                {
                    cells[cellToMove.x, cellToMove.y] = cellType.empty;
                    cells[emptyPlace.x, emptyPlace.y] = cellType.water;
                }
            }
        }

        // additional spreading
        /*
        for(int i = 1; i < width - 1; i++)
        {
            for(int j = 0; j < height - 1; j++)
            {
                if (cells[i, j] == cellType.water)
                {
                    if (cells[i, j + 1] != cellType.empty)
                    {
                        if (cells[i + 1, j] == cellType.empty && cells[i - 1, j] != cellType.empty)
                        {
                            cells[i, j] = cellType.empty;
                            cells[i + 1, j] = cellType.water;
                        }

                        if (cells[i - 1, j] == cellType.empty && cells[i + 1, j] != cellType.empty)
                        {
                            cells[i, j] = cellType.empty;
                            cells[i - 1, j] = cellType.water;
                        }
                    }
                }
            }
        }
        */
    }

    void Start()
    {

    }
        
    void Update()
    {

    }
}

