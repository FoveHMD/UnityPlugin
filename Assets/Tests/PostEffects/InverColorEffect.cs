using UnityEngine;

public class InverColorEffect : MonoBehaviour {

	public Material material;

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		Graphics.Blit(source, destination, material);
	}
}
