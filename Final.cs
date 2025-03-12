using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DungeonExplorer
{
    ///stores game wide constant values
    public static class GameConstants
    {
        public const int MaxHealth = 100;
        public const string None = "None";
        public const bool DebugMode = false;
        public const int EnemyDamage = 50;
    }

    ///represents an enemy 
    public class Enemy
    {
        public string Name { get; }
        public bool IsDefeated { get; private set; }
        private readonly Random _random;

        public Enemy(string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(name), "Enemy name cannot be null or empty");
            Name = name;
            _random = new Random();
        }

        public (int First, int Second, int Expected) GenerateMathProblem()
        {
            int first = _random.Next(1, 11);
            int second = _random.Next(1, 11);
            return (first, second, first * second);
        }

        public void Defeat() => IsDefeated = true;
    }

    ///represents a player in the game with health, inventory, and combat 
    public class Player
    {
        public string Name { get; }
        public int Health { get; private set; }
        public string Item { get; private set; } = GameConstants.None;
        //controls whether status messages are displayed
        private readonly bool _suppressMessages;

        ///creates a new player with specified name and health
        public Player(string name, int health, bool isTesting = false)
        {
            Name = string.IsNullOrEmpty(name) ? throw new ArgumentException("Name cannot be empty", nameof(name)) : name;
            Health = health is < 0 or > GameConstants.MaxHealth
                ? throw new ArgumentException($"Health must be between 0 and {GameConstants.MaxHealth}")
                : health;
            _suppressMessages = isTesting;
        }

        //returns true if players health is 0 or below
        public bool IsDead => Health <= 0;

        ///adds an item to the players inventory if they dont already have one
        public void PickUpItem(string newItem)
        {
            Debug.Assert(!string.IsNullOrEmpty(newItem), "Item cannot be null or empty");
            Debug.Assert(Item == GameConstants.None, "Player must not have an item to pick up a new one");

            if (string.IsNullOrEmpty(newItem))
                throw new ArgumentException("Item cannot be empty", nameof(newItem));

            if (Item == GameConstants.None)
            {
                Item = newItem;
                if (!_suppressMessages)
                    Console.WriteLine($"{Name} has picked up the {Item}.");
            }
        }

        ///removes and returns the current item from players inventory
        public string DropItem()
        {
            var droppedItem = Item;
            if (droppedItem != GameConstants.None && !_suppressMessages)
                Console.WriteLine($"{Name} has dropped the {droppedItem}.");

            Item = GameConstants.None;
            return droppedItem;
        }

        ///applies damage to the player and checks if they have died
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

    ///represents a room in the dungeon that can contain items and be explored
    public class Room
    {
        public string Name { get; }
        public string Description { get; }
        public string Item { get; private set; }
        public bool HasBeenInspected { get; private set; }
        public Enemy? CurrentEnemy { get; private set; }

        ///creates a new room with specified details
        public Room(string name, string description, string item)
        {
            Name = string.IsNullOrEmpty(name) ? throw new ArgumentException("Name cannot be empty", nameof(name)) : name;
            Description = string.IsNullOrEmpty(description) ? throw new ArgumentException("Description cannot be empty", nameof(description)) : description;
            Item = string.IsNullOrEmpty(item) ? throw new ArgumentException("Item cannot be empty", nameof(item)) : item;
        }

        public void SpawnEnemy(Enemy enemy)
        {
            Debug.Assert(enemy != null, "Enemy cannot be null");
            CurrentEnemy = enemy;
        }

        public void RemoveEnemy() => CurrentEnemy = null;

        ///returns the rooms description and mentions any items found
        public string GetDescription()
        {
            HasBeenInspected = true;
            string baseDescription = Item == GameConstants.None
                ? Description
                : $"{Description}\nYou seem to have found a {Item} here.";

            if (CurrentEnemy != null && !CurrentEnemy.IsDefeated)
            {
                return $"{baseDescription}\nBeware! A {CurrentEnemy.Name} is in this room!";
            }
            return baseDescription;
        }

        ///returns the initial description shown when entering a room
        public string GetInitialDescription() =>
            "You find yourself in a dimly lit, ruined room, its walls overgrown with vines.";

        ///places a new item in the room
        public void SetItem(string newItem) =>
            Item = string.IsNullOrEmpty(newItem) ? throw new ArgumentException("Item cannot be empty", nameof(newItem)) : newItem;

        /// Removes the current item from the room
        public void ClearItem() => Item = GameConstants.None;
    }

    ///main game class that manages game state and player interactions
    public class Game
    {
        private readonly Player _player;
        private readonly List<Room> _rooms;
        private Room _currentRoom;
        private readonly List<string> _exploredRooms;
        private bool _isRunning;
        private readonly Random _random;
        private Enemy? _currentEnemy;

        ///initialises a new game with a player name
        public Game(string playerName)
        {
            _player = new Player(string.IsNullOrEmpty(playerName) ? "Hero" : playerName, GameConstants.MaxHealth);
            _random = new Random();

            _rooms = new List<Room>
            {
                new("Starting Room", "A dimly lit dungeon room. There appears to be an weathered sword on the floor.", "Ancient Sword"),
                new("Dark Chamber", "A pitch-black chamber with cold stone walls. In the corner, you notice something leaning against the wall.", "Old Bow"),
                new("Mysterious Alcove", "A mysterious alcove covered in ancient runes. Something metallic gleams in the faint light.", "Shield")
            };

            _currentRoom = _rooms[0];
            _exploredRooms = new List<string>();
            _isRunning = true;

            //spawn enemy in either second or third room
            SpawnRandomEnemy();
        }
        ///spawns an enemy (Dark Knight) randomly in either the second or third room
        ///the enemy will not spawn in the starting room to give players a safe starting point
        private void SpawnRandomEnemy()
        {
            int roomIndex = _random.Next(1, 3); //generates 1 or 2 for second or third room
            _currentEnemy = new Enemy("Dark Knight");
            _rooms[roomIndex].SpawnEnemy(_currentEnemy);
        }
        ///handles the initial enemy encounter when entering a room
        ///presents the player with options to either fight or flee
        ///if the player makes an invalid choice, they take damage
        private void HandleEnemyEncounter()
        {
            var enemy = _currentRoom.CurrentEnemy;
            if (enemy == null || enemy.IsDefeated) return;

            Console.WriteLine($"\nYou've encountered a {enemy.Name}!");
            Console.WriteLine("What would you like to do?");
            Console.WriteLine("1. Attack");
            Console.WriteLine("2. Run Away");
            Console.Write("Enter your choice (1-2): ");

            var choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    HandleCombat(enemy);
                    break;
                case "2":
                    HandleRetreat();
                    break;
                default:
                    Console.WriteLine("Invalid choice. The enemy attacks you!");
                    _player.TakeDamage(GameConstants.EnemyDamage);
                    break;
            }
        }
        ///handles the combat sequence with an enemy
        ///presents the player with a multiplication math problem
        ///if solved correctly, the enemy is defeated
        ///if answered incorrectly, the player takes damage
        private void HandleCombat(Enemy enemy)
        {
            var problem = enemy.GenerateMathProblem();
            Console.WriteLine("\nQuick! Solve this math problem to attack!");
            Console.Write($"{problem.First} × {problem.Second} = ");

            if (int.TryParse(Console.ReadLine(), out int answer) && answer == problem.Expected)
            {
                Console.WriteLine("Correct! You defeated the enemy!");
                enemy.Defeat();
                _currentRoom.RemoveEnemy();
            }
            else
            {
                Console.WriteLine("Wrong answer! The enemy attacks you!");
                _player.TakeDamage(GameConstants.EnemyDamage);
            }
        }
        ///handles the retreat sequence when running from an enemy
        ///shows available rooms to escape to
        ///if a valid room is chosen, player is moved there
        ///if an invalid choice is made, player takes damage
        private void HandleRetreat()
        {
            var availableRooms = _rooms.Where(r => r != _currentRoom).ToList();
            Console.WriteLine("\nWhere would you like to run to?");

            for (int i = 0; i < availableRooms.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {availableRooms[i].Name}");
            }

            Console.Write($"Enter room number (1-{availableRooms.Count}): ");
            if (int.TryParse(Console.ReadLine(), out var roomChoice) &&
                roomChoice >= 1 && roomChoice <= availableRooms.Count)
            {
                ChangeRoom(availableRooms[roomChoice - 1]);
            }
            else
            {
                Console.WriteLine("Invalid choice. You stumble and the enemy attacks you!");
                _player.TakeDamage(GameConstants.EnemyDamage);
            }
        }

        /// Changes the current room and displays the new room's description
        private void ChangeRoom(Room newRoom)
        {
            Debug.Assert(newRoom != null, "New room cannot be null");
            Debug.Assert(newRoom != _currentRoom, "New room must be different from current room");

            _currentRoom = newRoom;
            Console.WriteLine($"\nYou enter {newRoom.Name}.");
            Console.WriteLine(newRoom.GetInitialDescription());

            // Handle enemy encounter after room description
            HandleEnemyEncounter();
        }

        /// Starts the game loop and handles player input until game over
        public void Start()
        {
            Console.WriteLine("Welcome to Dungeon Explorer!");
            Console.WriteLine("(Hint: Use option 2 to inspect the room carefully!)");
            Console.WriteLine(_currentRoom.GetInitialDescription());

            while (_isRunning && !_player.IsDead)
            {
                ShowMenu();
                ProcessChoice();
            }

            if (_player.IsDead)
                ShowGameOverStats();
        }

        // Displays final game statistics when the player dies
        private void ShowGameOverStats()
        {
            Console.WriteLine("\nGAME OVER");
            Console.WriteLine("\nFinal Adventure Summary:");
            Console.WriteLine($"Health: {_player.Health}/{GameConstants.MaxHealth}");
            Console.WriteLine($"Final Item: {_player.Item}");
            Console.WriteLine($"Rooms Explored: {_exploredRooms.Count}/{_rooms.Count}");

            if (_exploredRooms.Count > 0)
            {
                Console.WriteLine("\nRooms you discovered:");
                foreach (var roomName in _exploredRooms)
                    Console.WriteLine($"- {roomName}");
            }
        }

        // Displays the main game menu with available actions
        private void ShowMenu()
        {
            Console.WriteLine($"\nLocation: {_currentRoom.Name}");
            Console.WriteLine("\nWhat would you like to do?");
            Console.WriteLine("1. View Player Stats");
            Console.WriteLine("2. View Room Description");
            Console.WriteLine("3. Pick Up Item");
            Console.WriteLine("4. Drop Current Item");
            Console.WriteLine("5. Simulate Taking Damage");
            Console.WriteLine("6. Move to Another Room");
            Console.WriteLine("7. Exit Game");
            Console.Write("Enter your choice (1-7): ");
        }

        // Processes the player's menu selection
        private void ProcessChoice()
        {
            var choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1": ShowPlayerStats(); break;
                case "2": ShowRoomDescription(); break;
                case "3": AttemptPickupItem(); break;
                case "4": AttemptDropItem(); break;
                case "5": SimulateDamage(); break;
                case "6": AttemptRoomChange(); break;
                case "7":
                    _isRunning = false;
                    Console.WriteLine("Thanks for playing!");
                    break;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }
        }

        // Displays current player statistics
        private void ShowPlayerStats()
        {
            Console.WriteLine("Player Status:");
            Console.WriteLine($"Name: {_player.Name}");
            Console.WriteLine($"Health: {_player.Health}");
            Console.WriteLine($"Current Item: {_player.Item}");
        }

        // Shows the current room's description and marks it as explored
        private void ShowRoomDescription()
        {
            Console.WriteLine("Room Description:");
            Console.WriteLine(_currentRoom.GetDescription());
            if (!_exploredRooms.Contains(_currentRoom.Name))
            {
                _exploredRooms.Add(_currentRoom.Name);
            }
        }

        // Handles the logic for picking up items from the current room
        private void AttemptPickupItem()
        {
            Debug.Assert(_currentRoom != null, "Current room cannot be null");
            Debug.Assert(_player != null, "Player cannot be null");

            if (!_currentRoom.HasBeenInspected)
            {
                Console.WriteLine("You should inspect the room first (option 2) before trying to pick up anything.");
                return;
            }

            if (_currentRoom.Item == GameConstants.None)
            {
                Console.WriteLine("There is nothing to pick up in this room.");
                return;
            }

            if (_player.Item != GameConstants.None)
            {
                Console.WriteLine("You must drop your current item first.");
                return;
            }

            _player.PickUpItem(_currentRoom.Item);
            _currentRoom.ClearItem();
        }

        // Handles the logic for dropping items in the current room
        private void AttemptDropItem()
        {
            if (_player.Item == GameConstants.None)
            {
                Console.WriteLine("You have nothing to drop.");
                return;
            }

            if (_currentRoom.Item != GameConstants.None)
            {
                Console.WriteLine("The room already has an item in it. Can't drop your item here.");
                return;
            }

            var droppedItem = _player.DropItem();
            _currentRoom.SetItem(droppedItem);
        }

        // Simulates the player taking damage
        private void SimulateDamage()
        {
            Console.Write("Enter damage amount: ");
            if (int.TryParse(Console.ReadLine(), out var damage))
            {
                if (_player.TakeDamage(damage))
                    _isRunning = false;
            }
            else
            {
                Console.WriteLine("Invalid damage amount.");
            }
        }

        // Handles the logic for changing rooms
        private void AttemptRoomChange()
        {
            Debug.Assert(_rooms != null && _rooms.Count > 0, "Rooms list must be initialized and not empty");
            Debug.Assert(_currentRoom != null, "Current room cannot be null");

            Console.WriteLine("\nAvailable Rooms:");
            for (int i = 0; i < _rooms.Count; i++)
            {
                Debug.Assert(_rooms[i] != null, $"Room at index {i} cannot be null");
                Console.WriteLine($"{i + 1}. {_rooms[i].Name}");
            }

            Console.Write($"\nEnter room number (1-{_rooms.Count}): ");
            if (int.TryParse(Console.ReadLine(), out var roomChoice) &&
                roomChoice >= 1 && roomChoice <= _rooms.Count)
            {
                var newRoom = _rooms[roomChoice - 1];
                Debug.Assert(newRoom != null, "New room cannot be null");

                if (newRoom == _currentRoom)
                {
                    Console.WriteLine("You are already in this room!");
                    return;
                }
                ChangeRoom(newRoom);
            }
            else
            {
                Console.WriteLine("Invalid room choice.");
            }
        }
    }

    /// Entry point for the Dungeon Explorer game
    public static class Program
    {
        public static void Main()
        {
            Console.Write("Enter your hero's name: ");
            var game = new Game(Console.ReadLine());
            game.Start();
        }
    }
}
