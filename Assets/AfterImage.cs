using UnityEngine;

public class AfterImage : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    SpriteRenderer sr;
    float lifetime;
    float timer;
    Color startColor;

    public void Init(float life, Color color)
    {
        sr = GetComponent<SpriteRenderer>();
        lifetime = life;
        startColor = color;
        sr.color = color;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = timer / lifetime;
        sr.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * (1f - t));
        if (t >= 1f) Destroy(gameObject);
    }
}
