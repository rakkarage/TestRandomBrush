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
		private bool randomBool => UnityEngine.Random.value > .5f;
		private bool flipX => (orientation & Orientation.FlipX) == Orientation.FlipX ? randomBool : false;
		private bool flipY => (orientation & Orientation.FlipY) == Orientation.FlipY ? randomBool : false;
		private bool rot90 => (orientation & Orientation.Rot90) == Orientation.Rot90 ? randomBool : false;
		private static Quaternion rotateClockwise = Quaternion.Euler(0, 0, -90f);
		private static Quaternion rotateCounter = Quaternion.Euler(0, 0, 90f);
		private Matrix4x4 randomMatrix => Matrix4x4.TRS(Vector3.zero,
			rot90 ? rotateClockwise : Quaternion.identity, new Vector3(flipX ? -1f : 1f, flipY ? -1f : 1f, 1f));
		public TileBase[] randomTiles;
		public int[] probabilities;
		private TileBase randomTile
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
					cache[key] = new Tuple<TileBase, Matrix4x4>(randomTile, randomMatrix);
				return cache[key];
			}
			set
			{
				cache[key] = value;
				lastPosition = key;
			}
		}
		// TODO: FIX THIS SHIT!!! WTF!?
		public void CacheClear(BoundsInt bounds)
		{
			foreach (var i in bounds.allPositionsWithin)
				CacheClear(i);
		}
		public void CacheClear(Vector3Int position)
		{
			if (lastPosition != position)
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
		public bool moving { get; set; }
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
			Common(map, gridLayout, brushTarget, position);
			foreach (var i in cache.ToList())
				Common(map, gridLayout, brushTarget, i.Key);
		}
		private void Common(Tilemap map, GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
		{
			var c = this[position];
			map.SetTile(position, c.Item1);
			map.SetTransformMatrix(position, c.Item2);
		}
	}
	[CustomEditor(typeof(TestRandomBrush))]
	public class RandomBrushEditor : GridBrushEditor
	{
		private TestRandomBrush randomBrush => target as TestRandomBrush;
		private GameObject lastBrush;
		public override void PaintPreview(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
		{
			var brush = randomBrush;
			brush.CacheClear(position);
			if (brush.randomTiles?.Length > 0)
				Common(gridLayout, brushTarget, position);
		}
		public override void BoxFillPreview(GridLayout gridLayout, GameObject brushTarget, BoundsInt position)
		{
			var brush = randomBrush;
			brush.CacheClear(position);
			if (brush.randomTiles?.Length > 0)
				foreach (var i in position.allPositionsWithin)
					Common(gridLayout, brushTarget, i);
		}
		public override void FloodFillPreview(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
		{
			var brush = randomBrush;
			brush.CacheClear(position);
			if (brush.randomTiles?.Length > 0)
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
			var min = position - brush.pivot;
			var max = min + brush.size;
			var bounds = new BoundsInt(min, max - min);
			foreach (var i in bounds.allPositionsWithin)
				Common(map, gridLayout, brushTarget, i);
		}
		private void Common(Tilemap map, GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
		{
			var c = randomBrush[position];
			map.SetEditorPreviewTile(position, c.Item1);
			map.SetEditorPreviewTransformMatrix(position, c.Item2);
			lastBrush = brushTarget;
		}
		public override void ClearPreview()
		{
			if (lastBrush != null)
			{
				randomBrush.CacheUpdate();
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
			var brush = randomBrush;
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
