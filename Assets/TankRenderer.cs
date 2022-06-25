using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankRenderer : MonoBehaviour {

	public Tank tank;

	public Mesh wallModel, columnModel, surfaceHalf, surfaceFull;

	public Material wallMaterial, columnMaterial, waterMaterial;

	void Start () {
		for (int i = 0; i < tank.numCoordinates; i++) {
			int length;
			Direction dir = tank.GetDirection(i, out length);
		}
		tank.RefreshWater();
	}

	private const float SQRT2 = 1.414213562f;
	void Update () {

		DrawWalls();
		DrawWater();

	}

	void OnValidate () {
		//tank.RefreshWater();
	}

	private void DrawWalls () {
		for (int i = 0; i < tank.numCoordinates; i++) {
			int length;
			Direction dir = tank.GetDirection(i, out length);

			if (dir == Direction.Null) continue;

			bool isDiagonal = ((int)dir) % 2 == 1;

			float wallLength = length;
			if (isDiagonal) wallLength *= SQRT2;

			Vector2Int p = tank.wallCoordinates[i];
			Vector3 pos = new Vector3(p.x, 0, p.y);

			Quaternion rot = Quaternion.Euler(Vector3.up * (((int)dir) * -45 + 90));

			Matrix4x4 wallMat = Matrix4x4.identity, colMat = Matrix4x4.identity;

			wallMat.SetTRS(pos, rot, new Vector3(1, tank.tankHeight, wallLength));
			colMat.SetTRS(pos, Quaternion.identity, new Vector3(1, tank.tankHeight, 1));

			wallMat = transform.localToWorldMatrix * wallMat;
			colMat = transform.localToWorldMatrix * colMat;

			if (wallModel) Graphics.DrawMesh(wallModel, wallMat, wallMaterial, 0);
			if (columnModel) Graphics.DrawMesh(columnModel, colMat, columnMaterial, 0);
		}
	}

	private void DrawWater () {
		for (int x = 0; x < tank.size.x; x++) {
			for (int y = 0; y < tank.size.y; y++) {
				int value = tank.WaterIndex(x, y);
                if (value == Tank.WATER_CLEAR)
                    continue;

				Vector3 pos = new Vector3(x + 0.5f, tank.tankHeight - 0.1f, y + 0.5f);
				pos += new Vector3(tank.boundsMin.x, 0, tank.boundsMin.y);

				Matrix4x4 mat = Matrix4x4.identity;
                Quaternion rot = Quaternion.identity;
                
                if (value == Tank.WATER_CORNER1) rot = Quaternion.Euler(0, 270, 0);
                if (value == Tank.WATER_CORNER2) rot = Quaternion.Euler(0, 180, 0);
                if (value == Tank.WATER_CORNER3) rot = Quaternion.Euler(0, 90, 0);
                
				mat.SetTRS(pos, rot, Vector3.one);
                
                Mesh mesh = value == Tank.WATER_FULL ? surfaceFull : surfaceHalf;
                Graphics.DrawMesh(mesh, mat, waterMaterial, 0);
                
			}
		}
	}



	void OnDrawGizmosSelected () {
		tank.DrawGizmos();
	}

}

[System.Serializable]
public class Tank {

	public Vector2Int[] wallCoordinates;
	public float tankHeight;

	public int numCoordinates { get => wallCoordinates.Length; }

	public Vector2Int boundsMin { get; private set; }
	public Vector2Int boundsMax { get; private set; }
	public Vector2Int size { get => boundsMax - boundsMin; }
	public int tileCount { get => size.x * size.y; }

	private enum Coordinate { Air, Water, Wall }

	private int[] wallTexture = new int[0];
	private int[] waterTexture = new int[0];

	public void RefreshWater () {
		if (numCoordinates == 0) return;

		int minX = int.MaxValue, minY = int.MaxValue;
		int maxX = int.MinValue, maxY = int.MinValue;

		for (int i = 0; i < numCoordinates; i++) {
			Vector2Int p = wallCoordinates[i];
			if (p.x < minX) minX = p.x;
			if (p.x > maxX) maxX = p.x;
			if (p.y < minY) minY = p.y;
			if (p.y > maxY) maxY = p.y;
		}

		boundsMin = new Vector2Int(minX, minY);
		boundsMax = new Vector2Int(maxX, maxY);
		wallTexture = new int[tileCount];
		waterTexture = new int[tileCount];
		for (int i = 0; i < tileCount; i++) { waterTexture[i] = WATER_FULL; }

		FloodFill();
	}

	public void DrawGizmos () {
		for (int i = 0; i < numCoordinates; i++) {
			int j = (i + 1) % numCoordinates;
			Vector2Int a = wallCoordinates[i], b = wallCoordinates[j];

			Gizmos.color = Color.green;
			Gizmos.DrawLine(new Vector3(a.x, 0, a.y), new Vector3(b.x, 0, b.y));
			Gizmos.DrawLine(new Vector3(a.x, tankHeight, a.y), new Vector3(b.x, tankHeight, b.y));

			Gizmos.color = Color.blue;
			Gizmos.DrawLine(new Vector3(a.x, 0, a.y), new Vector3(a.x, tankHeight, a.y));

		}
		Gizmos.color = new Color(0, 0.5f, 1, 0.3f);

		for (int i = 0; i < waterTexture.Length; i++) {
			int y = i / size.x, x = i - y * size.x;
			Vector3 pos = new Vector3(x + 0.5f, tankHeight - 0.1f, y + 0.5f);
			pos += new Vector3(boundsMin.x, 0, boundsMin.y);

			int v = waterTexture[i];
			Gizmos.color = v == WATER_FULL ? Color.blue : v == WATER_CLEAR ? Color.red : Color.green;


			/*int v = wallTexture[i];
			Gizmos.color = Color.HSVToRGB(v / 16f, 1, 1);
            */
			//Gizmos.DrawCube(pos, new Vector3(1, 0.01f, 1));

		}
	}

	public Direction GetDirection (int index, out int length) {
		length = -1;

		Vector2Int a = wallCoordinates[index], b = wallCoordinates[(index + 1) % numCoordinates];

		int dx = b.x - a.x, dy = b.y - a.y;
		if (dx == 0 || dy == 0) {

			if (dx == 0 && dy == 0) {

				length = 0;
				return Direction.Null;

			} else if (dx == 0) {

				length = dy > 0 ? dy : -dy;
				return dy > 0 ? Direction.Up : Direction.Down;

			} else if (dy == 0) {

				length = dx > 0 ? dx : -dx;
				return dx > 0 ? Direction.Right : Direction.Left;

			}

		} else if (Mathf.Abs(dx) == Mathf.Abs(dy)) {

			if (dx == dy) {

				length = dx > 0 ? dx : -dx;
				return dx > 0 ? Direction.RightUp : Direction.LeftDown;

			} else {

				length = dx > dy ? dx : -dx;
				return dx > dy ? Direction.DownRight : Direction.UpLeft;

			}

		}

		return Direction.Null;
	}

	private int Index (int x, int y) {
		return y * size.x + x;
	}

	private const int
		RIGHT_BIT = 0,
		TOP_BIT = 1,
		LEFT_BIT = 2,
		BOTTOM_BIT = 3,
		DIAG_UP_BIT = 4,
		DIAG_DOWN_BIT = 5;
	public const int
		WATER_CORNER0 = 0,
		WATER_CORNER1 = 1,
		WATER_CORNER2 = 2,
		WATER_CORNER3 = 3,
		WATER_FULL = 4,
		WATER_CLEAR = -1;

	private void FloodFill () {

		//Mark walls on grid
		for (int c = 0; c < numCoordinates; c++) {

			int length;
			Direction dir = GetDirection(c, out length);
			Vector2Int v = wallCoordinates[c];

			v -= boundsMin;
			for (int i = 0; i < length; i++) {

				switch (dir) {
					case Direction.Right:
						SetWall(v.x, v.y, BOTTOM_BIT);
						SetWall(v.x, v.y - 1, TOP_BIT);

						v.x++;
						break;
					case Direction.RightUp:
						SetWall(v.x, v.y, DIAG_UP_BIT);

						v.x++; v.y++;
						break;
					case Direction.Up:
						SetWall(v.x, v.y, LEFT_BIT);
						SetWall(v.x - 1, v.y, RIGHT_BIT);

						v.y++;
						break;
					case Direction.UpLeft:
						SetWall(v.x - 1, v.y, DIAG_DOWN_BIT);

						v.x--; v.y++;
						break;
					case Direction.Left:
						SetWall(v.x - 1, v.y, BOTTOM_BIT);
						SetWall(v.x - 1, v.y - 1, TOP_BIT);

						v.x--;
						break;
					case Direction.LeftDown:
						SetWall(v.x - 1, v.y - 1, DIAG_UP_BIT);

						v.x--; v.y--;
						break;
					case Direction.Down:
						SetWall(v.x, v.y - 1, LEFT_BIT);
						SetWall(v.x - 1, v.y - 1, RIGHT_BIT);

						v.y--;
						break;
					case Direction.DownRight:
						SetWall(v.x, v.y - 1, DIAG_DOWN_BIT);

						v.x++; v.y--;
						break;
				}
			}

		}


		//Begin flood fill tests around edges
		//Flood test will skip over already flooded spaces
		for (int x = 0; x < size.x; x++) {
			if (!CheckWall(x, 0, BOTTOM_BIT)) FloodTest(x, 0);
			if (!CheckWall(x, size.y - 1, TOP_BIT)) FloodTest(x, size.y - 1);
		}
		for (int y = 1; y < size.y - 1; y++) {
			if (!CheckWall(0, y, LEFT_BIT)) FloodTest(0, y);
			if (!CheckWall(size.x - 1, y, RIGHT_BIT)) FloodTest(size.x - 1, y);
		}


		for (int c = 0; c < numCoordinates; c++) {

			Direction dir = GetDirection(c, out int length);
			Vector2Int v = wallCoordinates[c];

			Debug.Log(v);

			if (dir == Direction.Down || dir == Direction.Right || dir == Direction.Up || dir == Direction.Left)
				continue;

			v -= boundsMin;


			if (dir == Direction.UpLeft) {
				v.x--;
			} else if (dir == Direction.LeftDown) {
				v.x--; v.y--;
			} else if (dir == Direction.DownRight) {
				v.y--;
			}

			Debug.Log(dir + " " + length + ": " + v);

			for (int i = 0; i < length; i++) {

				if (v.x >= 0 && v.y >= 0 && v.x < size.x && v.y < size.y) {
					Debug.Log("Diag " + v);
					waterTexture[Index(v.x, v.y)] = CheckDiagonal(v.x, v.y, dir);
				}

				switch (dir) {
					case Direction.RightUp:
						v.x++; v.y++;
						break;
					case Direction.UpLeft:
						v.x--; v.y++;
						break;
					case Direction.LeftDown:
						v.x--; v.y--;
						break;
					case Direction.DownRight:
						v.x++; v.y--;
						break;
				}
			}

		}
	}

	private void SetWall (int x, int y, int value) {
		if (x < 0 || y < 0
			|| x >= size.x || y >= size.y)
			return;

		wallTexture[Index(x, y)] |= 1 << value;
	}

	private int CheckDiagonal (int x, int y, Direction dir) {

		if (x < 0 || y < 0
			|| x >= size.x || y >= size.y)
			return WATER_CLEAR;


		Debug.Log(x + ", " + y + ": " + dir);
		bool waterSideA = false, waterSideB = false;
		switch (dir) {
			case Direction.RightUp:
			case Direction.LeftDown:
				waterSideA = WaterIndex(x - 1, y) == WATER_FULL || WaterIndex(x, y + 1) == WATER_FULL;
				waterSideB = WaterIndex(x + 1, y) == WATER_FULL || WaterIndex(x, y - 1) == WATER_FULL;

				if (waterSideA == waterSideB) {
					return waterSideA ? WATER_FULL : WATER_CLEAR;
				} else {
					return waterSideA ? WATER_CORNER2 : WATER_CORNER0;
				}
			case Direction.UpLeft:
			case Direction.DownRight:
				waterSideA = WaterIndex(x + 1, y) == WATER_FULL || WaterIndex(x, y + 1) == WATER_FULL;
				waterSideB = WaterIndex(x - 1, y) == WATER_FULL || WaterIndex(x, y - 1) == WATER_FULL;

				if (waterSideA == waterSideB) {
					return waterSideA ? WATER_FULL : WATER_CLEAR;
				} else {
					return waterSideA ? WATER_CORNER1 : WATER_CORNER3;
				}


		}

		return WATER_CLEAR;
	}

	private static readonly Vector2Int[] NEIGHBORS = new Vector2Int[] { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };
	private void FloodTest (int x, int y) {
		if (waterTexture[Index(x, y)] != WATER_FULL)
			return;

		//Debug.Log("Flooded " + x + ", " + y);

		Queue<Vector2Int> floodQueue = new Queue<Vector2Int>();
		HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

		floodQueue.Enqueue(new Vector2Int(x, y));

		while (floodQueue.Count > 0) {
			Vector2Int coord = floodQueue.Dequeue();

			visited.Add(coord);
			waterTexture[Index(coord.x, coord.y)] = WATER_CLEAR;

			for (int n = 0; n < NEIGHBORS.Length; n++) {
				Vector2Int pos = coord + NEIGHBORS[n];
				if (InBounds(pos.x, pos.y) && !visited.Contains(pos) && !CheckWall(coord.x, coord.y, n)) {
					floodQueue.Enqueue(pos);
				}
			}

		}

		//Debug.Log("End");

	}

	public int WaterIndex (int x, int y) {
		if (x < 0 || y < 0
			|| x >= size.x || y >= size.y)
			return -1;

		return waterTexture[Index(x, y)];
	}

	private bool CheckWall (int x, int y, int side) {
		if (x < 0 || y < 0
			|| x >= size.x || y >= size.y)
			return true;


		int i = wallTexture[Index(x, y)];

		//Debug.Log(x + ", " + y + ": " + i + " with side " + side);

		bool isDiagonal = ((i & (1 << DIAG_DOWN_BIT)) != 0) || ((i & (1 << DIAG_UP_BIT)) != 0);

		return isDiagonal || (i & (1 << side)) != 0;
	}

	private bool InBounds (int x, int y) {
		return !(x < 0 || y < 0
			|| x >= size.x || y >= size.y);
	}
}

public enum Direction { Right, RightUp, Up, UpLeft, Left, LeftDown, Down, DownRight, Null = -1 }