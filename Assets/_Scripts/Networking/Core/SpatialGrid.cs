using System.Collections.Generic;
using UnityEngine;
using ExileSurvival.Networking.Entities;

namespace ExileSurvival.Networking.Core
{
    public class SpatialGrid
    {
        private readonly Dictionary<Vector2Int, List<NetworkEntity>> _cells = new Dictionary<Vector2Int, List<NetworkEntity>>();
        private readonly Dictionary<NetworkEntity, Vector2Int> _entityCellLookup = new Dictionary<NetworkEntity, Vector2Int>();
        private readonly float _cellSize;

        public SpatialGrid(float cellSize)
        {
            _cellSize = cellSize;
        }

        private Vector2Int GetCellCoords(Vector3 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x / _cellSize),
                Mathf.FloorToInt(position.z / _cellSize)
            );
        }

        public void Add(NetworkEntity entity)
        {
            var cellCoords = GetCellCoords(entity.transform.position);
            if (!_cells.ContainsKey(cellCoords))
            {
                _cells[cellCoords] = new List<NetworkEntity>();
            }
            _cells[cellCoords].Add(entity);
            _entityCellLookup[entity] = cellCoords;
        }

        public void Remove(NetworkEntity entity)
        {
            if (_entityCellLookup.TryGetValue(entity, out var cellCoords))
            {
                if (_cells.ContainsKey(cellCoords))
                {
                    _cells[cellCoords].Remove(entity);
                }
                _entityCellLookup.Remove(entity);
            }
        }

        public void UpdateEntityPosition(NetworkEntity entity)
        {
            if (!_entityCellLookup.ContainsKey(entity))
            {
                Add(entity);
                return;
            }

            var newCellCoords = GetCellCoords(entity.transform.position);
            var oldCellCoords = _entityCellLookup[entity];

            if (newCellCoords != oldCellCoords)
            {
                Remove(entity);
                Add(entity);
            }
        }

        public IEnumerable<NetworkEntity> GetEntitiesInRadius(Vector3 position, float radius)
        {
            var entitiesInRadius = new HashSet<NetworkEntity>();
            var centerCell = GetCellCoords(position);

            // Check a 3x3 grid of cells around the center
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    var cellCoords = new Vector2Int(centerCell.x + x, centerCell.y + y);
                    if (_cells.TryGetValue(cellCoords, out var cellEntities))
                    {
                        // This is a broad-phase check. We return all entities in the 9 cells.
                        // A more precise check would also check distance here, but this is a good starting point.
                        foreach (var entity in cellEntities)
                        {
                            entitiesInRadius.Add(entity);
                        }
                    }
                }
            }
            return entitiesInRadius;
        }
    }
}