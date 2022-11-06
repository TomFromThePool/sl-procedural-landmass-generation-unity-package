using UnityEngine;
using System.Collections;

public class MapPreview : MonoBehaviour {

	public Renderer textureRender;
	public MeshFilter meshFilter;
	public MeshRenderer meshRenderer;
	
	public enum DrawMode {NoiseMap, Mesh, FalloffMap, Live};
	public DrawMode drawMode;

	[Range(0,MeshSettings.numSupportedLODs-1)]
	public int editorPreviewLOD;
	public bool autoUpdate;

	public TerrainGenerator Generator;

	public int PreviewChunksX = 1;
	public int PreviewChunksY = 1;
	
	public void DrawMapInEditor() {
		Generator.ApplyTextureSettings();
		HeightMap heightMap = HeightMapGenerator.GenerateHeightMap (Generator.meshSettings.numVertsPerLine, Generator.meshSettings.numVertsPerLine, Generator.heightMapSettings, Vector2.zero);

		if (drawMode == DrawMode.NoiseMap) {
			DrawTexture (TextureGenerator.TextureFromHeightMap (heightMap));
		} else if (drawMode == DrawMode.Mesh) {
			DrawMesh (MeshGenerator.GenerateTerrainMesh (heightMap.values, Generator.meshSettings, editorPreviewLOD));
		} else if (drawMode == DrawMode.FalloffMap) {
			DrawTexture(TextureGenerator.TextureFromHeightMap(new HeightMap(FalloffGenerator.GenerateFalloffMap(Generator.meshSettings.numVertsPerLine),0,1)));
		} else if (drawMode == DrawMode.Live)
		{
			DrawTerrain();
		}
	}
	
	public void DrawTexture(Texture2D texture) {
		textureRender.sharedMaterial.mainTexture = texture;
		textureRender.transform.localScale = new Vector3 (texture.width, 1, texture.height) /10f;

		textureRender.gameObject.SetActive (true);
		meshFilter.gameObject.SetActive (false);
		Generator.Clear();
	}

	public void DrawMesh(MeshData meshData) {
		meshFilter.sharedMesh = meshData.CreateMesh ();
		textureRender.gameObject.SetActive (false);
		meshFilter.gameObject.SetActive (true);
		Generator.Clear();
	}

	public void DrawTerrain()
	{
		meshFilter.gameObject.SetActive(false);
		textureRender.gameObject.SetActive (false);
		meshFilter.gameObject.SetActive (false);
		Generator.GenerateChunks(PreviewChunksX, PreviewChunksY);
	}
	

	void OnValuesUpdated() {
		if (!Application.isPlaying) {
			DrawMapInEditor ();
		}
	}

	void OnTextureValuesUpdated() {
		Generator.textureSettings.ApplyToMaterial (Generator.mapMaterial);
	}

	void OnValidate() {

		if (PreviewChunksX < 1)
		{
			PreviewChunksX = 1;
		}

		if (PreviewChunksY < 1)
		{
			PreviewChunksY = 1;
		}
		
		if (Generator.meshSettings != null) {
			Generator.meshSettings.OnValuesUpdated -= OnValuesUpdated;
			Generator.meshSettings.OnValuesUpdated += OnValuesUpdated;
		}
		if (Generator.heightMapSettings != null) {
			Generator.heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
			Generator.heightMapSettings.OnValuesUpdated += OnValuesUpdated;
		}
		if (Generator.textureSettings != null) {
			Generator.textureSettings.OnValuesUpdated -= OnTextureValuesUpdated;
			Generator.textureSettings.OnValuesUpdated += OnTextureValuesUpdated;
		}

	}

}
