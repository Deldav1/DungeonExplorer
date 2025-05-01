public abstract class Creature
{
    public string Name { get; }
    public int Health { get; protected set; }
    public bool IsAlive => Health > 0;

    protected Creature(string name, int health)
    {
        Name = name;
        Health = health;
    }

    public abstract void Attack(Creature target);
    public virtual void TakeDamage(int damage)
    {
        Health = Math.Max(0, Health - damage);
        if (GameConstants.DebugMode)
            Console.WriteLine($"[DEBUG] {Name} took {damage} damage (HP: {Health})");
    }
}

public class Player : Creature
{
    public Inventory Inventory { get; }
    private readonly bool _suppressMessages;

    public Player(string name, int health, bool isTesting = false)
        : base(name, health)
    {
        Inventory = new Inventory();
        _suppressMessages = isTesting;
    }

    public override void Attack(Creature target)
    {
        var weapon = Inventory.Items.OfType<Weapon>().FirstOrDefault();
        int damage = weapon?.DamageBonus ?? GameConstants.BasePlayerDamage;
        target.TakeDamage(damage);
    }
}

public class Monster : Creature
{
    public int Damage { get; }

    public Monster(string name, int health, int damage)
        : base(name, health)
    {
        Damage = damage;
    }

    public override void Attack(Creature target)
    {
        if (IsAlive)
        {
            target.TakeDamage(Damage);
            Console.WriteLine($"The {Name} attacks you for {Damage} damage!");
        }
    }
}