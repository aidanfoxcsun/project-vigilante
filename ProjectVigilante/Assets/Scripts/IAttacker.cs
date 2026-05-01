// IAttacker.cs
// Enemies implement this alongside IDamageable.
// Call OnAttackSignaled when the wind-up animation begins.
// Call OnAttackCanceled if the attack is interrupted before landing.
public interface IAttacker
{
    event System.Action<IAttacker> OnAttackSignaled;
    event System.Action<IAttacker> OnAttackCanceled;
    void InterruptAttack(); // Called by PlayerCombat when a counter lands
}