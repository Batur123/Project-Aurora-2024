using UnityEngine;

public class SpriteMerger : MonoBehaviour
{
    public SpriteRenderer spriteRenderer1;
    public SpriteRenderer spriteRenderer2;

    void Start()
    {
        // Get textures from both sprites
        Texture2D texture1 = spriteRenderer1.sprite.texture;
        Texture2D texture2 = spriteRenderer2.sprite.texture;

        // Assume both textures have the same dimensions
        int width = Mathf.Max(texture1.width, texture2.width);
        int height = Mathf.Max(texture1.height, texture2.height);

        // Create a new texture
        Texture2D combinedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        // Clear the texture
        Color[] clearColors = new Color[width * height];
        for(int i = 0; i < clearColors.Length; i++) clearColors[i] = new Color(0,0,0,0);
        combinedTexture.SetPixels(clearColors);

        // Merge texture1
        combinedTexture.SetPixels(0, 0, texture1.width, texture1.height, texture1.GetPixels());

        // Merge texture2 on top
        combinedTexture.SetPixels(0, 0, texture2.width, texture2.height, texture2.GetPixels());

        combinedTexture.Apply();

        // Create a new sprite
        Sprite combinedSprite = Sprite.Create(combinedTexture, 
            new Rect(0, 0, combinedTexture.width, combinedTexture.height), 
            new Vector2(0.5f, 0.5f));

        // Assign to a new SpriteRenderer
        GameObject combinedObject = new GameObject("CombinedSprite");
        SpriteRenderer combinedRenderer = combinedObject.AddComponent<SpriteRenderer>();
        combinedRenderer.sprite = combinedSprite;
    }
}