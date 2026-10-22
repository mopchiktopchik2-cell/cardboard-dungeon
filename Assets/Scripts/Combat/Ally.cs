using UnityEngine;

public class Ally : MonoBehaviour
{
    private PaperActor visual;
    private float life;
    private float age;
    private float attackTimer;
    private Enemy target;

    public static Ally Summon(Vector2 pos)
    {
        var go = new GameObject("golem_ally");
        go.transform.position = pos;
        var a = go.AddComponent<Ally>();
        a.visual = PaperActor.Create(go.transform, Game.I.Art, "golem", 0.85f, Color.white, true, false);
        a.visual.SpawnPop();
        a.life = 12f;
        Game.I.Fx.Ring(pos, new Color(0.5f, 0.95f, 0.9f, 0.9f), 0.9f, 0.5f);
        Game.I.Fx.Dust(pos, 8);
        return a;
    }

    private void Update()
    {
        age += Time.deltaTime;
        life -= Time.deltaTime;
        if (life <= 0f)
        {
            Game.I.Fx.Dust(transform.position, 8);
            Destroy(gameObject);
            return;
        }

        if (target == null || target.Dead)
        {
            target = Game.I.Rooms.NearestEnemy(transform.position, 99f);
        }

        if (target != null && !target.Dead)
        {
            Vector2 delta = (Vector2)target.transform.position - (Vector2)transform.position;
            float dist = delta.magnitude;
            if (dist > 1.15f)
            {
                Vector2 move = delta.normalized * 3.1f * Time.deltaTime;
                transform.position = Game.I.Rooms.Move(transform.position, move, 0.38f);
                visual.Walking = true;
                visual.Face(delta.x);
            }
            else
            {
                visual.Walking = false;
                visual.Face(delta.x);
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    attackTimer = 1.1f;
                    target.TakeDamage(20f, transform.position, true, true);
                    Game.I.Fx.Slash(target.transform.position, delta);
                    Game.I.Fx.Dust(target.transform.position, 3, 0.7f);
                    visual.Pulse(0.12f, 0.15f);
                }
            }
        }
        else
        {
            visual.Walking = false;
        }

        float k = Mathf.Clamp01(age / 12f);
        if (k > 0.85f) visual.SetTint(new Color(1f, 1f, 1f, 1f - (k - 0.85f) * 6f));
    }
}
