public interface IDamager
{
    public float Damage {  get; set; }

    public void TakeDamage(IDamageable target);
}
