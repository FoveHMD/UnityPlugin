using Fove.Unity;
using UnityEngine;
using UnityEngine.UI;

public class AssignTextures : MonoBehaviour {

    public RawImage EyeImage;
    public RawImage PositionImage;
    public RawImage MirrorImage;

    void Update()
    {
        EyeImage.texture = FoveResearch.EyesTexture;
        PositionImage.texture = FoveResearch.PositionTexture;
        MirrorImage.texture = FoveResearch.MirrorTexture;
    }
}
