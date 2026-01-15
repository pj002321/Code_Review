using Cysharp.Threading.Tasks;
using Hunt;
using UnityEngine;

public interface IProjectile
{
    public void Init(ProjectileBase prefab);
    public void SetCollision(Vector2 size);
}
