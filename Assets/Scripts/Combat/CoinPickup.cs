using System.Collections.Generic;
using UnityEngine;

/// <summary>Монета, выпавшая из врага в бесконечном режиме.</summary>
public class CoinPickup : MonoBehaviour
{
    private static readonly List<CoinPickup> Active = new List<CoinPickup>();

    private int amount;
    private float age;
    private float seed;
    private Vector2 velocity;
    private SpriteRenderer sr;
    private bool collected;

    public static void Drop(Vector2 pos, int value)
    {
        if (value <= 0 || Game.I == null || Game.I.Art == null) return;
        var go = new GameObject("coin_pickup");
        go.transform.position = pos + Vector2.up * 0.35f;
        var coin = go.AddComponent<CoinPickup>();
        coin.amount = value;
        coin.seed = Random.value * 6.28f;
        coin.velocity = Random.insideUnitCircle.normalized * Random.Range(1.4f, 2.6f) + Vector2.up * 1.2f;
        coin.sr = go.AddComponent<SpriteRenderer>();
        coin.sr.sprite = Game.I.Art.World("coin");
        coin.sr.color = new Color(1f, 0.86f, 0.45f, 1f);
        coin.sr.sortingOrder = 1460;
        go.transform.localScale = Vector3.one * 0.52f;
        Active.Add(coin);
    }

    public static void CollectAll()
    {
        var copy = new List<CoinPickup>(Active);
        foreach (var coin in copy)
        {
            if (coin != null) coin.Collect();
        }
    }

    private void Update()
    {
        if (collected || Game.I == null) return;
        age += Time.deltaTime;
        if (age < 0.22f)
        {
            transform.position += (Vector3)(velocity * Time.deltaTime);
            velocity = Vector2.Lerp(velocity, Vector2.zero, Time.deltaTime * 7f);
        }

        var player = Game.I.Player;
        if (player != null && !player.Dead)
        {
            Vector2 toPlayer = (Vector2)player.transform.position + Vector2.up * 0.55f - (Vector2)transform.position;
            float dist = toPlayer.magnitude;
            if (age > 0.16f && dist < 5.2f)
            {
                transform.position += (Vector3)(toPlayer.normalized * Mathf.Lerp(3.5f, 13f, 1f - dist / 5.2f) *
                                                Time.deltaTime);
            }
            if (dist < 0.48f)
            {
                Collect();
                return;
            }
        }

        if (sr != null)
        {
            float bob = Mathf.Sin(age * 7f + seed) * 0.08f;
            transform.position += Vector3.up * (bob * Time.deltaTime);
            sr.color = new Color(1f, 0.86f, 0.45f, age < 0.18f ? age / 0.18f : 1f);
            transform.localScale = Vector3.one * (0.52f + Mathf.Sin(age * 9f + seed) * 0.035f);
        }
        if (age > 25f) Destroy(gameObject);
    }

    private void Collect()
    {
        if (collected) return;
        collected = true;
        Active.Remove(this);
        if (Game.I != null && Game.I.Run != null)
        {
            Game.I.Run.AddCoins(amount);
            if (Game.I.Fx != null)
            {
                Game.I.Fx.FloatingText(transform.position + Vector3.up * 0.35f, "+" + amount,
                    new Color(1f, 0.85f, 0.38f), 0.8f);
            }
            if (Game.I.Audio != null) Game.I.Audio.Play("sfx_pickup", 0.55f, 1.25f);
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        Active.Remove(this);
    }
}
