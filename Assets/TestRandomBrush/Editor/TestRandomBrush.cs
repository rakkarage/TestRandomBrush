using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace UnityEditor
{
	[CreateAssetMenu, CustomGridBrush(false, true, false, "Test Random Brush")]
	public class TestRandomBrush : GridBrush
	{
		[Flags]
		public enum Orientation
		{
			None = 0,
			FlipX = 1,
			FlipY = (1 << 1),
			Rot90 = (1 << 2),
		}
		public Orientation orientation = Orientation.None;
		private bool RandomBool => UnityEngine.Random.value > .5f;
		private bool FlipX => (orientation & Orientation.FlipX) == Orientation.FlipX ? RandomBool : false;
		private bool FlipY => (orientation & Orientation.FlipY) == Orientation.FlipY ? RandomBool : false;
		private bool Rot90 => (orientation & Orientation.Rot90) == Orientation.Rot90 ? RandomBool : false;
		private static Quaternion rotateClockwise = Quaternion.Euler(0, 0, -90f);
		private static Quaternion rotateCounter = Quaternion.Euler(0, 0, 90f);
		private Matrix4x4 RandomMatrix => Matrix4x4.TRS(Vector3.zero,
			Rot90 ? rotateClockwise : Quaternion.identity, new Vector3(FlipX ? -1f : 1f, FlipY ? -1f : 1f, 1f));
		public TileBase[] randomTiles;
		public int[] probabilities;
		private TileBase RandomTile
		{
			get
			{
				if (probabilities?.Length == randomTiles.Length)
				{
					var total = 0f;
					var roll = UnityEngine.Random.Range(0, probabilities.Sum());
					for (var i = 0; i < probabilities.Length; i++)
					{
						total += probabilities[i];
						if (roll < total)
							return randomTiles[i];
					}
					return null;
				}
				else
					return randomTiles[UnityEngine.Random.Range(0, randomTiles.Length)];
			}
		}
		private Vector3Int? lastPosition;
		private Dictionary<Vector3Int, Tuple<TileBase, Matrix4x4>> cache = new Dictionary<Vector3Int, Tuple<TileBase, Matrix4x4>>();
		public Tuple<TileBase, Matrix4x4> this[Vector3Int key]
		{
			get
			{
				if (!cache.ContainsKey(key))
					cache[key] = new Tuple<TileBase, Matrix4x4>(RandomTile, RandomMatrix);
				return cache[key];
			}
			set
			{
				cache[key] = value;
				lastPosition = key;
			}
		}
		public void CacheClear(Vector3Int? position = null)
		{
			if (position == null || position != lastPosition)
				cache.Clear();
		}
		private FlipAxis? flipAxis;
		private RotationDirection? rotationDirection;
		public void CacheUpdate()
		{
			if (flipAxis.HasValue || rotationDirection.HasValue)
			{
				foreach (var i in cache.ToList())
				{
					var m = i.Value.Item2;
					var p = m.GetColumn(3);
					var r = Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
					var s = new Vector3(m.GetColumn(0).magnitude, m.GetColumn(1).magnitude, m.GetColumn(2).magnitude);
					if (flipAxis.HasValue)
						s = new Vector3(flipAxis.Value == FlipAxis.X ? s.x * -1f : s.x,
							flipAxis.Value == FlipAxis.Y ? s.y * -1f : s.y, s.z);
					if (rotationDirection.HasValue)
						r *= rotationDirection.Value == RotationDirection.Clockwise ? rotateClockwise : rotateCounter;
					m.SetTRS(p, r, s);
					cache[i.Key] = new Tuple<TileBase, Matrix4x4>(i.Value.Item1, m);
				}
				flipAxis = null;
				rotationDirection = null;
			}
		}
		public override void Flip(FlipAxis flip, GridLayout.CellLayout layout)
		{
			flipAxis = flip;
			base.Flip(flip, layout);
		}
		public override void Rotate(RotationDirection direction, GridLayout.CellLayout layout)
		{
			rotationDirection = direction;
			base.Rotate(direction, layout);
		}
		public bool Moving { get; set; }
		public override void MoveStart(GridLayout gridLayout, GameObject brushTarget, BoundsInt position)
		{
			Debug.Log("MoveStart");
			base.MoveStart(gridLayout, brushTarget, position);
		}
		public override void Move(GridLayout gridLayout, GameObject brushTarget, BoundsInt from, BoundsInt to)
		{
			Debug.Log("Move");
			base.Move(gridLayout, brushTarget, from, to);
		}
		public override void MoveEnd(GridLayout gridLayout, GameObject brushTarget, BoundsInt position)
		{
			Debug.Log("MoveEnd");
			base.MoveEnd(gridLayout, brushTarget, position);
		}
		public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
		{
			if (randomTiles?.Length > 0)
				Common(gridLayout, brushTarget, position);
		}
		public override void BoxFill(GridLayout gridLayout, GameObject brushTarget, BoundsInt position)
		{
			if (randomTiles?.Length > 0)
				foreach (var i in position.allPositionsWithin)
					Common(gridLayout, brushTarget, i);
		}
		public override void FloodFill(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
		{
			if (randomTiles?.Length > 0)
			{
				var map = brushTarget?.GetComponent<Tilemap>();
				if (map == null)
					return;
				FloodFill(map, gridLayout, brushTarget, position);
			}
		}
		private void FloodFill(Tilemap map, GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
		{
			var exist = map.GetTile(position);
			var points = new Stack<Vector3Int>();
			var used = new List<Vector3Int>();
			points.Push(position);
			while (points.Count > 0)
			{
				var p = points.Pop();
				used.Add(p);
				Common(map, gridLayout, brushTarget, p);
				for (var y = p.y - 1; y <= p.y + 1; y++)
				{
					for (var x = p.x - 1; x <= p.x + 1; x++)
					{
						var test = new Vector3Int(x, y, p.z);
						if ((test.y != p.y || test.x != p.x) && map.cellBounds.Contains(test) &&
							(exist ? map.GetTile(test) : !map.GetTile(test)) && !used.Contains(test))
							points.Push(test);
					}
				}
			}
		}
		private void Common(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
		{
			var map = brushTarget?.GetComponent<Tilemap>();
			if (map == null)
				return;
			var min = position - this.pivot;
			var max = min + this.size;
			var bounds = new BoundsInt(min, max - min);
			foreach (var i in bounds.allPositionsWithin)
				Common(map, gridLayout, brushTarget, i);
		}
		private void Common(Tilemap map, GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
		{
			var c = this[position];
			map.SetTile(position, c.Item1);
			map.SetTransformMatrix(position, c.Item2);
		}
	}
	[CustomEditor(typeof(TestRandomBrush))]
	public class TestRandomBrushEditor : GridBrushEditor
	{
		private TestRandomBrush RandomBrush => target as TestRandomBrush;
		private GameObject lastBrush;
		public override void PaintPreview(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
		{
			if (RandomBrush.randomTiles?.Length > 0)
				Common(gridLayout, brushTarget, position);
		}
		public override void BoxFillPreview(GridLayout gridLayout, GameObject brushTarget, BoundsInt position)
		{
			if (RandomBrush.randomTiles?.Length > 0)
				foreach (var i in position.allPositionsWithin)
					Common(gridLayout, brushTarget, i);
		}
		public override void FloodFillPreview(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
		{
			if (RandomBrush.randomTiles?.Length > 0)
			{
				var map = brushTarget?.GetComponent<Tilemap>();
				if (map == null)
					return;
				FloodFill(map, gridLayout, brushTarget, position);
			}
		}
		private void FloodFill(Tilemap map, GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
		{
			var exist = map.GetTile(position);
			var points = new Stack<Vector3Int>();
			var used = new List<Vector3Int>();
			var bounds = map.cellBounds;
			var size = new Vector2Int((int)map.cellSize.x, (int)map.cellSize.y);
			points.Push(position);
			while (points.Count > 0)
			{
				var p = points.Pop();
				used.Add(p);
				if (!bounds.Contains(p))
					bounds.SetMinMax(new Vector3Int(Math.Min(bounds.xMin, p.x), Math.Min(bounds.yMin, p.y), 0),
						new Vector3Int(Math.Max(bounds.xMax, p.x) + size.x, Math.Max(bounds.yMax, p.y) + size.y, 1));
				Common(map, gridLayout, brushTarget, p);
				for (var y = p.y - 1; y <= p.y + 1; y++)
				{
					for (var x = p.x - 1; x <= p.x + 1; x++)
					{
						var test = new Vector3Int(x, y, p.z);
						if ((test.y != p.y || test.x != p.x) && map.cellBounds.Contains(test) &&
							(exist ? map.GetTile(test) : !map.GetTile(test)) && !used.Contains(test))
							points.Push(test);
					}
				}
			}
		}
		private void Common(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
		{
			var map = brushTarget?.GetComponent<Tilemap>();
			if (map == null)
				return;
			var min = position - brush.pivot;
			var max = min + brush.size;
			var bounds = new BoundsInt(min, max - min);
			foreach (var i in bounds.allPositionsWithin)
				Common(map, gridLayout, brushTarget, i);
		}
		private void Common(Tilemap map, GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
		{
			var c = RandomBrush[position];
			map.SetEditorPreviewTile(position, c.Item1);
			map.SetEditorPreviewTransformMatrix(position, c.Item2);
			lastBrush = brushTarget;
		}
		public override void ClearPreview()
		{
			if (lastBrush != null)
			{
				RandomBrush.CacheUpdate();
				var map = lastBrush.GetComponent<Tilemap>();
				if (map == null)
					return;
				map.ClearAllEditorPreviewTiles();
				lastBrush = null;
			}
		}
		public override void OnPaintInspectorGUI() => GUI();
		public override void OnInspectorGUI() => GUI();
		private void GUI()
		{
			var brush = RandomBrush;
			EditorGUI.BeginChangeCheck();
			brush.orientation = (TestRandomBrush.Orientation)EditorGUILayout.EnumFlagsField("Random Orientation", brush.orientation);
			if (EditorGUI.EndChangeCheck())
				EditorUtility.SetDirty(brush);
			var so = serializedObject;
			EditorGUILayout.PropertyField(so.FindProperty(nameof(brush.randomTiles)), true);
			if (so.ApplyModifiedProperties())
				EditorUtility.SetDirty(brush);
			EditorGUILayout.PropertyField(so.FindProperty(nameof(brush.probabilities)), true);
			if (so.ApplyModifiedProperties())
				EditorUtility.SetDirty(brush);
		}
	}
}
