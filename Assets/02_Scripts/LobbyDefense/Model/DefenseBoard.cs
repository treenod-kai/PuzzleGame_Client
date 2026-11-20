using System;
using System.Collections.Generic;

namespace LobbyDefense.Model
{
    /// <summary>방어선의 칸 점유와 출구까지의 경로를 관리한다. 칸 번호는 왼쪽 위부터 행 순서로 0에서 시작한다.</summary>
    public sealed class DefenseBoard
    {
        /// <summary>각 칸에서 출구까지 통과할 최소 유닛 수와 그 조건에서의 최소 걸음 수를 저장한다.</summary>
        private sealed class Routes
        {
            public readonly int[] Walls;
            public readonly int[] Steps;

            /// <summary>전체 칸 수 <paramref name="count"/>에 맞춰 거리표를 준비한다.</summary>
            public Routes(int count)
            {
                Walls = new int[count];
                Steps = new int[count];
            }
        }

        private readonly bool[] _occupied;
        private readonly bool[] _previewOccupied;
        private readonly Routes _current;
        private readonly Routes _preview;
        private readonly Queue<int> _frontier;
        private readonly Queue<int> _beyondWall;
        private bool _routesDirty = true;

        public int Width { get; }
        public int Height { get; }
        public int CellCount => _occupied.Length;
        public int Entrance { get; }
        public int Exit { get; }
        public int Revision { get; private set; }

        /// <summary>가로 <paramref name="width"/>, 세로 <paramref name="height"/>인 빈 보드에
        /// 입구 칸 <paramref name="entrance"/>와 출구 칸 <paramref name="exit"/>를 지정한다.</summary>
        public DefenseBoard(int width, int height, int entrance, int exit)
        {
            if (width <= 0 || height <= 0 || (long)width * height > int.MaxValue)
            {
                throw new ArgumentException("보드의 가로와 세로 크기를 확인하세요.");
            }
            int count = width * height;
            if (entrance < 0 || entrance >= count || exit < 0 || exit >= count || entrance == exit)
            {
                throw new ArgumentException("입구와 출구는 보드 안의 서로 다른 칸이어야 합니다.");
            }

            Width = width;
            Height = height;
            Entrance = entrance;
            Exit = exit;
            _occupied = new bool[count];
            _previewOccupied = new bool[count];
            _current = new Routes(count);
            _preview = new Routes(count);
            _frontier = new Queue<int>(count);
            _beyondWall = new Queue<int>(count);
        }

        /// <summary>칸 번호 <paramref name="cell"/>이 보드 안에 있는지 확인한다.</summary>
        public bool Contains(int cell) => cell >= 0 && cell < CellCount;

        /// <summary>칸 <paramref name="cell"/>에 방어 유닛이 있는지 확인한다.</summary>
        public bool IsOccupied(int cell) => Contains(cell) && _occupied[cell];

        /// <summary>칸 <paramref name="cell"/>이 배치 가능한 빈칸인지 확인한다.</summary>
        public bool CanPlace(int cell) => Contains(cell) && cell != Entrance && cell != Exit && !_occupied[cell];

        /// <summary>빈칸 <paramref name="cell"/>을 유닛이 점유한다.</summary>
        public bool TryPlace(int cell)
        {
            if (!CanPlace(cell))
            {
                return false;
            }
            _occupied[cell] = true;
            _routesDirty = true;
            Revision++;
            return true;
        }

        /// <summary>유닛이 점유한 칸 <paramref name="cell"/>을 비운다.</summary>
        public bool TryRemove(int cell)
        {
            if (!IsOccupied(cell))
            {
                return false;
            }
            _occupied[cell] = false;
            _routesDirty = true;
            Revision++;
            return true;
        }

        /// <summary>두 칸 <paramref name="from"/>과 <paramref name="to"/> 사이의 상하좌우 거리를 구한다.</summary>
        public int GetManhattanDistance(int from, int to)
        {
            if (!Contains(from) || !Contains(to))
            {
                throw new ArgumentException("거리를 구할 칸이 보드 밖에 있습니다.");
            }
            return Math.Abs(from % Width - to % Width) + Math.Abs(from / Width - to / Width);
        }

        /// <summary>빈칸만 따라 <paramref name="cell"/>에서 출구까지 가는 걸음 수를 구한다. 봉쇄되었거나 보드 밖이면 -1이다.</summary>
        public int GetDistance(int cell)
        {
            RefreshRoutes();
            if (!Contains(cell) || _current.Walls[cell] != 0)
            {
                return -1;
            }
            return _current.Steps[cell];
        }

        /// <summary>출구로 가려는 <paramref name="cell"/>의 다음 칸을 구한다. 점유 칸이면 공격하고 출구나 보드 밖이면 -1이다.</summary>
        public int GetNextCell(int cell)
        {
            RefreshRoutes();
            return NextCell(cell, _current, _occupied);
        }

        /// <summary>시작 칸 <paramref name="start"/>에서 출구까지 이동하거나 돌파할 칸을 양 끝을 포함해 <paramref name="path"/>에 채운다.</summary>
        public void GetPath(int start, List<int> path)
        {
            RefreshRoutes();
            FillPath(start, _current, _occupied, path);
        }

        /// <summary>후보 <paramref name="candidate"/>를 점유했을 때 입구부터의 경로를 <paramref name="path"/>에 채우고
        /// 돌파가 필요한지 <paramref name="isBlocked"/>로 반환한다. 배치할 수 없는 칸이면 실패한다.</summary>
        public bool TryPreview(int candidate, List<int> path, out bool isBlocked) => TryPreview(candidate, -1, path, out isBlocked);

        /// <summary>마지막으로 계산한 배치 후보에서 <paramref name="cell"/>의 적이 출구까지 가려면 벽을 공격해야 하는지 확인한다.</summary>
        internal bool IsBlockedInPreview(int cell) => Contains(cell) && _preview.Walls[cell] > 0;

        /// <summary>기존 칸 <paramref name="vacated"/>을 비우고 후보 <paramref name="candidate"/>에 배치할 때의 경로 <paramref name="path"/>와
        /// 입구 봉쇄 여부 <paramref name="isBlocked"/>를 구한다. 새 배치의 기존 칸은 -1이다.</summary>
        public bool TryPreview(int candidate, int vacated, List<int> path, out bool isBlocked)
        {
            if (!CanPlace(candidate) || (vacated != -1 && !IsOccupied(vacated)))
            {
                path.Clear();
                isBlocked = false;
                return false;
            }
            Array.Copy(_occupied, _previewOccupied, CellCount);
            if (vacated != -1)
            {
                _previewOccupied[vacated] = false;
            }
            _previewOccupied[candidate] = true;
            BuildRoutes(_preview, _previewOccupied);
            FillPath(Entrance, _preview, _previewOccupied, path);
            isBlocked = _preview.Walls[Entrance] > 0;
            return true;
        }

        /// <summary>점유가 바뀐 뒤 처음 경로를 요청할 때 확정 거리표를 갱신한다.</summary>
        private void RefreshRoutes()
        {
            if (_routesDirty)
            {
                BuildRoutes(_current, _occupied);
                _routesDirty = false;
            }
        }

        /// <summary>점유 상태 <paramref name="occupied"/>로 출구에서 역방향 탐색한 결과를 <paramref name="routes"/>에 채운다.</summary>
        private void BuildRoutes(Routes routes, bool[] occupied)
        {
            for (int i = 0; i < CellCount; i++)
            {
                routes.Walls[i] = -1;
                routes.Steps[i] = -1;
            }

            // 먼저 빈칸으로 이어진 영역을 모두 방문한 뒤 유닛 한 기를 더 통과하는 영역으로 넘어간다.
            Queue<int> frontier = _frontier;
            Queue<int> beyondWall = _beyondWall;
            frontier.Enqueue(Exit);
            routes.Walls[Exit] = 0;
            while (frontier.Count > 0)
            {
                int cell = frontier.Dequeue();
                for (int direction = 0; direction < 4; direction++)
                {
                    int neighbor = Neighbor(cell, direction);
                    if (neighbor < 0 || routes.Walls[neighbor] >= 0)
                    {
                        continue;
                    }
                    bool wall = occupied[neighbor];
                    routes.Walls[neighbor] = routes.Walls[cell] + (wall ? 1 : 0);
                    if (wall)
                    {
                        beyondWall.Enqueue(neighbor);
                    }
                    else
                    {
                        frontier.Enqueue(neighbor);
                    }
                }
                if (frontier.Count == 0)
                {
                    Queue<int> finished = frontier;
                    frontier = beyondWall;
                    beyondWall = finished;
                }
            }

            // 최소 돌파 횟수가 보장된 간선만 따라 너비 우선 탐색하면 그중 가장 짧은 걸음 수가 나온다.
            _frontier.Enqueue(Exit);
            routes.Steps[Exit] = 0;
            while (_frontier.Count > 0)
            {
                int cell = _frontier.Dequeue();
                for (int direction = 0; direction < 4; direction++)
                {
                    int neighbor = Neighbor(cell, direction);
                    if (neighbor < 0 || routes.Steps[neighbor] >= 0)
                    {
                        continue;
                    }
                    int wallCost = occupied[neighbor] ? 1 : 0;
                    if (routes.Walls[neighbor] == routes.Walls[cell] + wallCost)
                    {
                        routes.Steps[neighbor] = routes.Steps[cell] + 1;
                        _frontier.Enqueue(neighbor);
                    }
                }
            }
        }

        /// <summary>점유 상태 <paramref name="occupied"/>의 거리표 <paramref name="routes"/>에서
        /// 시작 칸 <paramref name="start"/>부터 이어지는 경로를 <paramref name="path"/>에 담는다.</summary>
        private void FillPath(int start, Routes routes, bool[] occupied, List<int> path)
        {
            path.Clear();
            int cell = start;
            while (Contains(cell))
            {
                path.Add(cell);
                cell = NextCell(cell, routes, occupied);
            }
        }

        /// <summary>점유 상태 <paramref name="occupied"/>의 거리표 <paramref name="routes"/>에서
        /// 현재 칸 <paramref name="cell"/>의 다음 칸을 고른다. 동률이면 오른쪽, 위, 아래, 왼쪽 순서다.</summary>
        private int NextCell(int cell, Routes routes, bool[] occupied)
        {
            if (!Contains(cell) || cell == Exit)
            {
                return -1;
            }
            int wallCost = occupied[cell] ? 1 : 0;
            int remainingWalls = routes.Walls[cell] - wallCost;
            int remainingSteps = routes.Steps[cell] - 1;
            for (int direction = 0; direction < 4; direction++)
            {
                int neighbor = Neighbor(cell, direction);
                if (neighbor >= 0 && routes.Walls[neighbor] == remainingWalls && routes.Steps[neighbor] == remainingSteps)
                {
                    return neighbor;
                }
            }
            return -1;
        }

        /// <summary>칸 <paramref name="cell"/>에서 우선순위 <paramref name="direction"/> 방향의 이웃을 구한다. 보드 밖이면 -1이다.</summary>
        private int Neighbor(int cell, int direction)
        {
            switch (direction)
            {
                case 0: return cell % Width < Width - 1 ? cell + 1 : -1;
                case 1: return cell >= Width ? cell - Width : -1;
                case 2: return cell < CellCount - Width ? cell + Width : -1;
                default: return cell % Width > 0 ? cell - 1 : -1;
            }
        }
    }
}
