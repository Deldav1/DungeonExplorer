using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DungeonExplorer
{
    /// <summary>
    /// Stores game-wide constant values used throughout the application
    /// </summary>
    public static class GameConstants
    {
        public const int MaxHealth = 100;
        public const string None = "None";
        public const bool DebugMode = false;

        // New constants
        public const int MaxInventorySize = 5;
        public static readonly string[] PotionTypes = { "Health", "Strength", "Invisibility" };
        public static readonly string[] WeaponTypes = { "Sword", "Axe", "Bow", "Dagger" };
        public static readonly string[] MonsterTypes = { "Goblin", "Orc", "Skeleton", "Spider" };
        public const int BasePlayerDamage = 3; // Bare hands damage
    }

    /// <summary>
    /// Represents a monster in the game with health and combat capabilities
    /// </summary>
    public class Monster
    {
        public string Name { get; }
        public int Health { get; private set; }
        public int Damage { get; }
        public bool IsAlive => Health > 0;

        public Monster(string name, int health, int damage)
        {
            Name = name;
            Health = health;
            Damage = damage;
        }

        public void TakeDamage(int damage)
        {
            if (!GameConstants.DebugMode)
            {
                Health = Math.Max(0, Health - damage);
                return;
            }

            Health = Math.Max(0, Health - damage);
            Console.WriteLine($"[DEBUG] {Name} took {damage} damage (HP: {Health})");
        }

        public void Attack(Player player)
        {
            if (IsAlive)
            {
                player.TakeDamage(Damage);
                Console.WriteLine($"The {Name} attacks you for {Damage} damage!");
            }
        }
    }

    /// <summary>
    /// Base class for all items in the game
    /// </summary>
    public abstract class Item
    {
        public string Name { get; }
        public string Description { get; }

        protected Item(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public abstract void Use(Player player);
    }

    /// <summary>
    /// Represents a weapon that can be equipped
    /// </summary>
    public class Weapon : Item
    {
        public int DamageBonus { get; }

        public Weapon(string name, int damageBonus)
            : base(name, $"A {name} that increases attack damage by {damageBonus}")
        {
            DamageBonus = damageBonus;
        }

        public override void Use(Player player)
        {
            Console.WriteLine($"You equip the {Name}. Damage increased by {DamageBonus}!");
        }
    }

    /// <summary>
    /// Represents a consumable potion
    /// </summary>
    public class Potion : Item
    {
        public int EffectValue { get; }
        public string PotionType { get; }

        public Potion(string type, int value)
            : base($"{type} Potion", $"Restores {value} {type} points")
        {
            PotionType = type;
            EffectValue = value;
        }

        public override void Use(Player player)
        {
            switch (PotionType)
            {
                case "Health":
                    player.Health = Math.Min(GameConstants.MaxHealth, player.Health + EffectValue);
                    Console.WriteLine($"You drink the {Name} and restore {EffectValue} health!");
                    break;
                default:
                    Console.WriteLine($"You use the {Name}, but nothing happens.");
                    break;
            }
        }
    }

    /// <summary>
    /// Manages the player's collection of items
    /// </summary>
    public class Inventory
    {
        private readonly List<Item> _items = new();

        public IReadOnlyList<Item> Items => _items.AsReadOnly();
        public bool IsFull => _items.Count >= GameConstants.MaxInventorySize;

        public bool AddItem(Item item)
        {
            if (IsFull) return false;

            _items.Add(item);
            return true;
        }

        public bool RemoveItem(Item item)
        {
            return _items.Remove(item);
        }

        public Item GetItem(int index)
        {
            return index >= 0 && index < _items.Count ? _items[index] : null;
        }

        public void Display()
        {
            if (_items.Count == 0)
            {
                Console.WriteLine("Your inventory is empty.");
                return;
            }

            Console.WriteLine("Inventory:");
            for (int i = 0; i < _items.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {_items[i].Name} - {_items[i].Description}");
            }
        }
    }

    /// <summary>
    /// Represents an enemy in the game (legacy, replaced by Monster)
    /// </summary>
    public class Enemy
    {
        public string Name { get; }
        public bool IsDefeated { get; private set; }

        public Enemy(string name)
        {
            Name = name;
        }

        public void Defeat() => IsDefeated = true;
    }

    /// <summary>
    /// Represents a player in the game with health, inventory, and combat capabilities
    /// </summary>
    public class Player
    {
        public string Name { get; }
        public int Health { get; set; }
        public Inventory Inventory { get; }
        private readonly bool _suppressMessages;

        public Player(string name, int health, bool isTesting = false)
        {
            Name = string.IsNullOrEmpty(name) ? throw new ArgumentException("Name cannot be empty", nameof(name)) : name;
            Health = health is < 0 or > GameConstants.MaxHealth
                ? throw new ArgumentException($"Health must be between 0 and {GameConstants.MaxHealth}")
                : health;
            _suppressMessages = isTesting;
            Inventory = new Inventory();
        }

        public bool IsDead => Health <= 0;

        public bool TakeDamage(int damage)
        {
            Debug.Assert(damage >= 0, "Damage must be non-negative");
            Debug.Assert(Health > 0, "Player must be alive to take damage");

            if (damage <= 0) return IsDead;

            Health = Math.Max(0, Health - damage);

            if (!_suppressMessages)
            {
                Console.WriteLine($"{Name} took {damage} damage. Health is now {Health}.");
                if (IsDead)
                    Console.WriteLine($"\n{Name} has fallen in the dungeon...");
            }

            return IsDead;
        }
    }

    /// <summary>
    /// Manages room connections and navigation
    /// </summary>
    public class GameMap
    {
        private readonly Dictionary<Room, List<Room>> _connections = new();
        private Room? _startingRoom;

        public void AddRoom(Room room, bool isStartingRoom = false)
        {
            if (!_connections.ContainsKey(room))
            {
                _connections[room] = new List<Room>();
                if (isStartingRoom) _startingRoom = room;
            }
        }

        public void ConnectRooms(Room room1, Room room2, bool bidirectional = true)
        {
            if (!_connections.ContainsKey(room1)) AddRoom(room1);
            if (!_connections.ContainsKey(room2)) AddRoom(room2);

            _connections[room1].Add(room2);
            if (bidirectional) _connections[room2].Add(room1);
        }

        public IEnumerable<Room> GetConnectedRooms(Room room)
        {
            return _connections.TryGetValue(room, out var connected)
                ? connected
                : Enumerable.Empty<Room>();
        }

        public Room GetStartingRoom()
        {
            if (_startingRoom == null)
                throw new InvalidOperationException("Starting room has not been set.");
            return _startingRoom;
        }
    }

    /// <summary>
    /// Represents a room in the dungeon that can contain items and monsters
    /// </summary>
    public class Room
    {
        public string Name { get; }
        public string Description { get; }
        public Item RoomItem { get; private set; }
        public bool HasBeenInspected { get; private set; }
        public List<Monster> Monsters { get; } = new();

        public Room(string name, string description, Item item)
        {
            Name = string.IsNullOrEmpty(name) ? throw new ArgumentException("Name cannot be empty", nameof(name)) : name;
            Description = string.IsNullOrEmpty(description) ? throw new ArgumentException("Description cannot be empty", nameof(description)) : description;
            RoomItem = item ?? throw new ArgumentNullException(nameof(item));
        }

        public void SpawnMonster(Monster monster)
        {
            Debug.Assert(monster != null, "Monster cannot be null");
            Monsters.Add(monster);
        }

        public void RemoveMonster(Monster monster) => Monsters.Remove(monster);

        public string GetDescription()
        {
            HasBeenInspected = true;
            string baseDescription = RoomItem == null
                ? Description
                : $"{Description}\nYou notice a {RoomItem.Name} here.";

            if (Monsters.Any(m => m.IsAlive))
            {
                var monsters = Monsters.Where(m => m.IsAlive).ToList();
                baseDescription += $"\nThere {(monsters.Count > 1 ? "are" : "is")} {monsters.Count} " +
                                   $"{(monsters.Count > 1 ? "monsters" : "monster")} lurking here!";
            }

            return baseDescription;
        }

        public string GetInitialDescription() =>
            $"You enter {Name}. " +
            "The air is thick with dust and the scent of ancient stone.";

        public void SetItem(Item newItem) =>
            RoomItem = newItem ?? throw new ArgumentNullException(nameof(newItem));

        public void ClearItem() => RoomItem = default!;
    }

    /// <summary>
    /// Main game class that manages game state and player interactions
    /// </summary>
    public class Game
    {
        private readonly Player _player;
        private readonly GameMap _map = new();
        private Room _currentRoom;
        private readonly List<string> _exploredRooms;
        private bool _isRunning;
        private readonly Random _random;

        public Game(string playerName)
        {
            _player = new Player(string.IsNullOrEmpty(playerName) ? "Hero" : playerName, GameConstants.MaxHealth);
            _random = new Random();

            // Create weapons in order of power
            var weapons = new List<Weapon>
            {
                new("Rusty Dagger", 5),
                new("Bronze Sword", 8),
                new("Steel Axe", 12),
                new("Enchanted Bow", 16),
                new("Dragonbone Greatsword", 20)
            };

            // Create rooms with progressively better loot
            var rooms = new List<Room>
            {
                new("Entrance Hall", "The crumbling entrance to the ancient dungeon.", weapons[0]),
                new("Guard Room", "A room with rusted weapons racks and broken armor.", new Potion("Health", 20)),
                new("Armory", "A room filled with broken weapons racks.", weapons[1]),
                new("Treasure Vault", "A glittering vault filled with ancient artifacts.", weapons[2]),
                new("Royal Chambers", "Opulent but decayed living quarters.", new Potion("Health", 30)),
                new("Throne Room", "A massive hall with a dark throne at its center.", weapons[4])
            };

            // Set up map connections (linear progression)
            _map.AddRoom(rooms[0], true);
            for (int i = 1; i < rooms.Count; i++)
            {
                _map.AddRoom(rooms[i]);
                _map.ConnectRooms(rooms[i - 1], rooms[i]);
            }

            // Spawn enemies appropriate for each room
            SpawnEnemy(rooms[1], 0); // Goblin
            SpawnEnemy(rooms[2], 1); // Skeleton
            SpawnEnemy(rooms[3], 1); // Skeleton
            SpawnEnemy(rooms[4], 2); // Orc
            SpawnEnemy(rooms[5], 3); // Dark Knight

            _currentRoom = _map.GetStartingRoom();
            _exploredRooms = new List<string>();
            _isRunning = true;
        }

        private void SpawnEnemy(Room room, int difficultyLevel)
        {
            var enemies = new List<Monster>
            {
                new("Goblin Scout", 30, 5),
                new("Skeleton Warrior", 50, 8),
                new("Orc Berserker", 80, 12),
                new("Dark Knight", 120, 15)
            };

            difficultyLevel = Math.Clamp(difficultyLevel, 0, enemies.Count - 1);
            room.SpawnMonster(new Monster(
                enemies[difficultyLevel].Name,
                enemies[difficultyLevel].Health,
                enemies[difficultyLevel].Damage));
        }

        private void HandleCombat(Monster monster)
        {
            Console.WriteLine($"\n=== BATTLE WITH {monster.Name.ToUpper()} ===");
            Console.WriteLine($"Your Health: {_player.Health}/{GameConstants.MaxHealth}");
            Console.WriteLine($"Enemy Health: {monster.Health}");

            while (!_player.IsDead && monster.IsAlive)
            {
                Console.WriteLine("\nChoose action:");
                Console.WriteLine("1. Attack");
                Console.WriteLine("2. Use Item");
                Console.WriteLine("3. Attempt to Flee");
                Console.Write("Your choice: ");

                switch (Console.ReadLine())
                {
                    case "1": // Attack
                        var weapon = _player.Inventory.Items.OfType<Weapon>().FirstOrDefault();
                        int damage = weapon?.DamageBonus ?? GameConstants.BasePlayerDamage;
                        monster.TakeDamage(damage);
                        Console.WriteLine($"You strike with {weapon?.Name ?? "bare hands"} for {damage} damage!");
                        break;

                    case "2": // Use Item
                        UseInventoryItem();
                        continue; // Skip enemy turn

                    case "3": // Flee
                        if (_random.Next(0, 2) == 0)
                        {
                            Console.WriteLine("You successfully escape!");
                            return;
                        }
                        Console.WriteLine("Escape failed!");
                        break;

                    default:
                        Console.WriteLine("Invalid choice! You hesitate...");
                        break;
                }

                // Enemy turn if still alive
                if (monster.IsAlive)
                {
                    monster.Attack(_player);
                    if (_player.IsDead) break;
                }
                else
                {
                    Console.WriteLine($"\nYou defeated the {monster.Name}!");
                    _currentRoom.RemoveMonster(monster);
                }

                // Update status
                Console.WriteLine($"\n{_player.Name}: {_player.Health}/{GameConstants.MaxHealth} HP");
                Console.WriteLine($"{monster.Name}: {(monster.IsAlive ? monster.Health + " HP" : "DEFEATED")}");
            }

            if (_player.IsDead)
            {
                Console.WriteLine("\nYou have been defeated...");
            }
        }

        private void HandleMonsters()
        {
            var aliveMonsters = _currentRoom.Monsters.Where(m => m.IsAlive).ToList();
            if (!aliveMonsters.Any()) return;

            Console.WriteLine($"\n{aliveMonsters.Count} monster(s) attack!");
            foreach (var monster in aliveMonsters)
            {
                HandleCombat(monster);
                if (_player.IsDead) break;
            }
        }

        private void ChangeRoom(Room newRoom)
        {
            _currentRoom = newRoom;
            Console.WriteLine(newRoom.GetInitialDescription());

            if (!_exploredRooms.Contains(newRoom.Name))
            {
                _exploredRooms.Add(newRoom.Name);
            }

            HandleMonsters();
        }

        public void Start()
        {
            Console.WriteLine("=== DUNGEON EXPLORER ===");
            Console.WriteLine("(Type the number of any action to perform it)\n");
            Console.WriteLine(_currentRoom.GetInitialDescription());

            while (_isRunning && !_player.IsDead)
            {
                ShowMenu();
                ProcessChoice();
            }

            if (_player.IsDead)
            {
                ShowGameOverStats();
            }
            else
            {
                Console.WriteLine("\nThanks for playing Dungeon Explorer!");
            }
        }

        private void ShowGameOverStats()
        {
            Console.WriteLine("\n=== GAME OVER ===");
            Console.WriteLine("\nAdventure Summary:");
            Console.WriteLine($"- Health: {_player.Health}/{GameConstants.MaxHealth}");
            Console.WriteLine($"- Rooms Explored: {_exploredRooms.Count}");

            if (_exploredRooms.Count > 0)
            {
                Console.WriteLine("\nDiscovered Rooms:");
                foreach (var room in _exploredRooms)
                {
                    Console.WriteLine($"* {room}");
                }
            }
        }

        private void ShowMenu()
        {
            Console.WriteLine($"\n=== {_currentRoom.Name.ToUpper()} ===");
            Console.WriteLine("\nAvailable Actions:");
            Console.WriteLine("1. View Player Status");
            Console.WriteLine("2. Inspect Room");
            Console.WriteLine("3. Pick Up Item");
            Console.WriteLine("4. Drop Item");
            Console.WriteLine("5. Use Item");
            Console.WriteLine("6. View Inventory");
            Console.WriteLine("7. Move to Another Room");
            Console.WriteLine("8. Quit Game");
            Console.Write("\nWhat will you do? ");
        }

        private void ProcessChoice()
        {
            var choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1": ShowPlayerStats(); break;
                case "2": InspectRoom(); break;
                case "3": PickUpItem(); break;
                case "4": DropItem(); break;
                case "5": UseInventoryItem(); break;
                case "6": _player.Inventory.Display(); break;
                case "7": MoveToRoom(); break;
                case "8": _isRunning = false; break;
                default: Console.WriteLine("Invalid choice!"); break;
            }
        }

        private void ShowPlayerStats()
        {
            Console.WriteLine($"PLAYER: {_player.Name}");
            Console.WriteLine($"Health: {_player.Health}/{GameConstants.MaxHealth}");

            var weapon = _player.Inventory.Items.OfType<Weapon>().FirstOrDefault();
            Console.WriteLine($"Weapon: {weapon?.Name ?? "None"} " +
                             $"(Damage: {weapon?.DamageBonus ?? GameConstants.BasePlayerDamage})");

            Console.WriteLine($"Potions: {_player.Inventory.Items.OfType<Potion>().Count()}");
        }

        private void InspectRoom()
        {
            Console.WriteLine(_currentRoom.GetDescription());
        }

        private void PickUpItem()
        {
            if (_currentRoom.RoomItem == null)
            {
                Console.WriteLine("There's nothing to pick up here.");
                return;
            }

            if (_player.Inventory.IsFull)
            {
                Console.WriteLine("Your inventory is full! Drop something first.");
                return;
            }

            var item = _currentRoom.RoomItem;
            if (_player.Inventory.AddItem(item))
            {
                Console.WriteLine($"You picked up the {item.Name}!");
                _currentRoom.ClearItem();
            }
        }

        private void DropItem()
        {
            if (_player.Inventory.Items.Count == 0)
            {
                Console.WriteLine("Your inventory is empty.");
                return;
            }

            if (_currentRoom.RoomItem != null)
            {
                Console.WriteLine("This room already has an item. You can't drop anything here.");
                return;
            }

            Console.WriteLine("Select item to drop:");
            _player.Inventory.Display();
            Console.Write("Item number: ");

            if (int.TryParse(Console.ReadLine(), out int index) &&
                index > 0 && index <= _player.Inventory.Items.Count)
            {
                var item = _player.Inventory.GetItem(index - 1);
                _currentRoom.SetItem(item);
                _player.Inventory.RemoveItem(item);
                Console.WriteLine($"You dropped the {item.Name}.");
            }
            else
            {
                Console.WriteLine("Invalid selection.");
            }
        }

        private void UseInventoryItem()
        {
            if (_player.Inventory.Items.Count == 0)
            {
                Console.WriteLine("Your inventory is empty.");
                return;
            }

            Console.WriteLine("Select item to use:");
            _player.Inventory.Display();
            Console.Write("Item number: ");

            if (int.TryParse(Console.ReadLine(), out int index) &&
                index > 0 && index <= _player.Inventory.Items.Count)
            {
                var item = _player.Inventory.GetItem(index - 1);
                item.Use(_player);

                if (item is Potion)
                {
                    _player.Inventory.RemoveItem(item);
                }
            }
            else
            {
                Console.WriteLine("Invalid selection.");
            }
        }

        private void MoveToRoom()
        {
            var connectedRooms = _map.GetConnectedRooms(_currentRoom).ToList();
            if (connectedRooms.Count == 0)
            {
                Console.WriteLine("There are no exits from this room!");
                return;
            }

            Console.WriteLine("Nearby Rooms:");
            for (int i = 0; i < connectedRooms.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {connectedRooms[i].Name}");
            }

            Console.Write($"\nWhere to? (1-{connectedRooms.Count}): ");
            if (int.TryParse(Console.ReadLine(), out int choice) &&
                choice >= 1 && choice <= connectedRooms.Count)
            {
                ChangeRoom(connectedRooms[choice - 1]);
            }
            else
            {
                Console.WriteLine("Invalid room choice.");
            }
        }
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Welcome to Dungeon Explorer");

            Console.Write("\nEnter your hero's name: ");
            var input = Console.ReadLine();
            var playerName = string.IsNullOrWhiteSpace(input) ? "Hero" : input;
            var game = new Game(playerName);
            game.Start();
        }
    }
}