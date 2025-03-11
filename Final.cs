using System;
using System.Collections.Generic;

namespace DungeonExplorer
{
    /// Stores game-wide constant values used throughout the application
    public static class GameConstants
    {
        public const int MaxHealth = 100;
        public const string None = "None";
        public const bool DebugMode = false;
    }

    /// Represents a player in the game with health, inventory, and combat capabilities
    public class Player
    {
        public string Name { get; }
        public int Health { get; private set; }
        public string Item { get; private set; } = GameConstants.None;
        // Controls whether status messages are displayed
        private readonly bool _suppressMessages;

        /// Creates a new player with specified name and health
        public Player(string name, int health, bool isTesting = false)
        {
            Name = string.IsNullOrEmpty(name) ? throw new ArgumentException("Name cannot be empty", nameof(name)) : name;
            Health = health is < 0 or > GameConstants.MaxHealth 
                ? throw new ArgumentException($"Health must be between 0 and {GameConstants.MaxHealth}") 
                : health;
            _suppressMessages = isTesting;
        }

        // Returns true if player's health is 0 or below
        public bool IsDead => Health <= 0;

        /// Adds an item to the player's inventory if they don't already have one
        public void PickUpItem(string newItem)
        {
            if (string.IsNullOrEmpty(newItem))
                throw new ArgumentException("Item cannot be empty", nameof(newItem));

            if (Item == GameConstants.None)
            {
                Item = newItem;
                if (!_suppressMessages)
                    Console.WriteLine($"{Name} has picked up the {Item}.");
            }
        }

        /// Removes and returns the current item from player's inventory
        public string DropItem()
        {
            var droppedItem = Item;
            if (droppedItem != GameConstants.None && !_suppressMessages)
                Console.WriteLine($"{Name} has dropped the {droppedItem}.");
            
            Item = GameConstants.None;
            return droppedItem;
        }

        /// Applies damage to the player and checks if they died
        public bool TakeDamage(int damage)
        {
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

    /// Represents a room in the dungeon that can contain items and be explored
    public class Room
    {
        public string Name { get; }
        public string Description { get; }
        public string Item { get; private set; }
        public bool HasBeenInspected { get; private set; }

        /// Creates a new room with specified details
        public Room(string name, string description, string item)
        {
            Name = string.IsNullOrEmpty(name) ? throw new ArgumentException("Name cannot be empty", nameof(name)) : name;
            Description = string.IsNullOrEmpty(description) ? throw new ArgumentException("Description cannot be empty", nameof(description)) : description;
            Item = string.IsNullOrEmpty(item) ? throw new ArgumentException("Item cannot be empty", nameof(item)) : item;
        }

        /// Returns the room's description and mentions any items found
        public string GetDescription()
        {
            HasBeenInspected = true;
            return Item == GameConstants.None 
                ? Description 
                : $"{Description}\nYou seem to have found a {Item} here.";
        }

        /// Returns the initial description shown when entering a room
        public string GetInitialDescription() => 
            "You find yourself in a dimly lit, ruined room, its walls overgrown with vines.";

        /// Places a new item in the room
        public void SetItem(string newItem) => 
            Item = string.IsNullOrEmpty(newItem) ? throw new ArgumentException("Item cannot be empty", nameof(newItem)) : newItem;

        /// Removes the current item from the room
        public void ClearItem() => Item = GameConstants.None;
    }

    /// Main game class that manages game state and player interactions
    public class Game
    {
        private readonly Player _player;
        private readonly List<Room> _rooms;
        private Room _currentRoom;
        private readonly List<string> _exploredRooms;
        private bool _isRunning;

        /// Initializes a new game with a player name
        public Game(string playerName)
        {
            _player = new Player(string.IsNullOrEmpty(playerName) ? "Hero" : playerName, GameConstants.MaxHealth);
            // Initialize game with a single starting room (more rooms can be added)
            _rooms = new List<Room>
            {
                new("Starting Room", "A dimly lit dungeon room. There appears to be an weathered sword on the floor.", "Ancient Sword")
            };
            _currentRoom = _rooms[0];
            _exploredRooms = new List<string>();
            _isRunning = true;
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
            Console.WriteLine("6. Exit Game");
            Console.Write("Enter your choice (1-6): ");
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
                case "6":
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

        /// Changes the current room and displays the new room's description
        private void ChangeRoom(Room newRoom)
        {
            _currentRoom = newRoom;
            Console.WriteLine($"\nYou enter {newRoom.Name}.");
            Console.WriteLine(newRoom.GetInitialDescription());
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
