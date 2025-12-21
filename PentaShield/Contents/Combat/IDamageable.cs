public interface IDamageable
{
    public float Health { get; set; }
    public float MaxHealth { get; set; }
    public void OnHit(float damage);
    public void OnDie();

}
