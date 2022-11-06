using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TerrainGenerator : MonoBehaviour {

	const float viewerMoveThresholdForChunkUpdate = 25f;
	const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;


	public int colliderLODIndex;
	public LODInfo[] detailLevels;

	public MeshSettings meshSettings;
	public HeightMapSettings heightMapSettings;
	public TextureData textureSettings;

	public Transform viewer;
	public Material mapMaterial;

	Vector2 viewerPosition;
	Vector2 viewerPositionOld;

	float meshWorldSize;
	int chunksVisibleInViewDst;

	Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

	void Start()
	{
		ApplyTextureSettings();

		float maxViewDst = detailLevels [detailLevels.Length - 1].visibleDstThreshold;
		meshWorldSize = meshSettings.meshWorldSize;
		chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / meshWorldSize);

		UpdateVisibleChunks ();
	}

	public void ApplyTextureSettings()
	{
		textureSettings.ApplyToMaterial (mapMaterial);
		textureSettings.UpdateMeshHeights (mapMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
	}

	void Update() {
		viewerPosition = new Vector2 (viewer.position.x, viewer.position.z);

		if (viewerPosition != viewerPositionOld) {
			foreach (TerrainChunk chunk in visibleTerrainChunks) {
				chunk.UpdateCollisionMesh ();
			}
		}

		if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate) {
			viewerPositionOld = viewerPosition;
			UpdateVisibleChunks ();
		}
	}

	public TerrainChunk GetChunk(Vector2 coord, out bool wasCreated, bool alwaysVisible = false)
	{
		if (terrainChunkDictionary.ContainsKey (coord))
		{
			wasCreated = false;
			return terrainChunkDictionary[coord];
		}
		else
		{
			wasCreated = true;
			return CreateChunk(coord, alwaysVisible);
		}
	}

	public IEnumerable<TerrainChunk> AllChunks => terrainChunkDictionary.Values; 

	private TerrainChunk CreateChunk(Vector2 coord, bool alwaysVisible = false)
	{
		TerrainChunk newChunk = new TerrainChunk (coord,heightMapSettings,meshSettings, detailLevels, colliderLODIndex, transform, viewer, mapMaterial, alwaysVisible);
		terrainChunkDictionary.Add (coord, newChunk);
		return newChunk;
	}

	public Vector2 ViewedChunkCoord => new Vector2(Mathf.RoundToInt(viewerPosition.x / meshWorldSize),
		Mathf.RoundToInt(viewerPosition.y / meshWorldSize));
		
	void UpdateVisibleChunks() {
		HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2> ();
		for (int i = visibleTerrainChunks.Count-1; i >= 0; i--) {
			alreadyUpdatedChunkCoords.Add (visibleTerrainChunks [i].coord);
			visibleTerrainChunks [i].UpdateTerrainChunk ();
		}

		Vector2 chunkCoord = ViewedChunkCoord;
		int currentChunkCoordX = (int)chunkCoord.x;
		int currentChunkCoordY = (int)chunkCoord.y;

		for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++) {
			for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) {
				Vector2 viewedChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
				if (!alreadyUpdatedChunkCoords.Contains (viewedChunkCoord))
				{
					ChunkAt(viewedChunkCoord);
				}
			}
		}
	}

	void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible) {
		if (isVisible) {
			visibleTerrainChunks.Add (chunk);
		} else {
			visibleTerrainChunks.Remove (chunk);
		}
	}

	private TerrainChunk ChunkAt(Vector2 coord, bool alwaysVisible = false)
	{
		TerrainChunk chunk = GetChunk(coord, out bool wasCreated, alwaysVisible);
		if (!wasCreated)
		{
			chunk.UpdateTerrainChunk();
		}
		else
		{
			chunk.onVisibilityChanged += OnTerrainChunkVisibilityChanged;
			chunk.Load ();
		}

		return chunk;
	}
	
	public void GenerateChunks(int chunksX, int chunksY)
	{
		Clear();
		for (int y = 0; y < chunksY; y++)
		{
			for (int x = 0; x < chunksX; x++)
			{
				Vector2 viewedChunkCoord = new Vector2 (x, y);
				ChunkAt(viewedChunkCoord, true);
			}
		}
	}

	public void Clear()
	{
		foreach (var c in terrainChunkDictionary.Values)
		{
			c.onVisibilityChanged -= OnTerrainChunkVisibilityChanged;
			c.Destroy();
		}
		
		terrainChunkDictionary.Clear();

		if (transform.childCount > 0)
		{
			var children = new GameObject[transform.childCount];
			for(int i = 0; i < transform.childCount; i++)
			{
				children[i] = transform.GetChild(i).gameObject;
			}

			foreach (var g in children)
			{
				if (Application.isPlaying)
				{
					GameObject.Destroy(g);
				}
				else
				{
					GameObject.DestroyImmediate(g);
				}
			}
		}
	}

}

[System.Serializable]
public struct LODInfo {
	[Range(0,MeshSettings.numSupportedLODs-1)]
	public int lod;
	public float visibleDstThreshold;


	public float sqrVisibleDstThreshold {
		get {
			return visibleDstThreshold * visibleDstThreshold;
		}
	}
}
