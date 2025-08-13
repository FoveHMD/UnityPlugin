using Fove.Unity;
using UnityEngine;
using UnityEngine.UI;

public class AssignTextures : MonoBehaviour {

    public RawImage EyesImage;
    public RawImage PositionImage;
    public RawImage MirrorImage;

    public Text EyesImageStatus;
    public Text PosImageStatus;

    private bool eyesImage = false;
    private bool positionImage = false;

    void Start()
    {
        Material greyMat = new Material(Shader.Find("Fove/UnlitGreyShader"));
        EyesImage.material = greyMat;
        PositionImage.material = greyMat;
        EnableEyesImage(true);
        EnablePosImage(true);
    }

    void Update()
    {
        // Listen to input and update the capabilities
        if (Input.GetKeyUp(KeyCode.E))
            EnableEyesImage(!eyesImage);

        if (Input.GetKeyUp(KeyCode.P))
            EnablePosImage(!positionImage);

        // Update the image
        if (eyesImage)
            EyesImage.texture = FoveManager.GetEyesImage();
        if (positionImage)
            PositionImage.texture = FoveManager.GetPositionImage();

        MirrorImage.texture = FoveManager.GetMirrorTexture();
    }

    void EnableEyesImage(bool enable)
    {
        if (enable)
            FoveManager.RegisterCapabilities(Fove.ClientCapabilities.EyesImage);
        else
            FoveManager.UnregisterCapabilities(Fove.ClientCapabilities.EyesImage);

        eyesImage = enable;
        EyesImageStatus.text = enable ? "ON" : "OFF";
    }

    void EnablePosImage(bool enable)
    {
        if (enable)
            FoveManager.RegisterCapabilities(Fove.ClientCapabilities.PositionImage);
        else
            FoveManager.UnregisterCapabilities(Fove.ClientCapabilities.PositionImage);

        positionImage = enable;
        PosImageStatus.text = enable ? "ON" : "OFF";
    }
}
