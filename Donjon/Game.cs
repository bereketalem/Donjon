﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Donjon.Entities.Creatures;
using Donjon.Entities.Items;
using Donjon.Utils;
using Environment = Donjon.Entities.Environment;

namespace Donjon
{
    internal class Game
    {
        private readonly Level level;
        private readonly Log log = new Log();

        private readonly Ui ui;
        private bool gameInProgress = true;
        private Hero hero;

        public Game()
        {
            var map = new Map(width: 12, height: 12);
            ui = new Ui(log, map);
            level = new Level(map, log);
        }

        internal void Run()
        {
            Init();
            Draw();
            do
            {
                log.Archive();

                PlayerActions();
                if (!gameInProgress) break;

                GameActions();
                if (!gameInProgress) break;

                Cleanup();
            } while (gameInProgress);

            log.Add("Game Over");
            Draw();
        }

        private void Cleanup()
        {
            level.Cleanup();
        }

        private void GameActions()
        {
            var monsters = level.Monsters;
            foreach (var monster in monsters)
            {
                var acted = monster.Action();
                if (acted)
                {
                    Thread.Sleep(millisecondsTimeout: 500);
                    Draw();
                }

                if (hero.IsDead)
                {
                    gameInProgress = false;
                    break;
                }
            }
        }

        private void PlayerActions()
        {
            var acted = false;
            do
            {
                var key = Console.ReadKey(intercept: true).Key;
                ui.MenuClear();
                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        acted = MoveHero(Direction.N);
                        break;
                    case ConsoleKey.DownArrow:
                        acted = MoveHero(Direction.S);
                        break;
                    case ConsoleKey.LeftArrow:
                        acted = MoveHero(Direction.W);
                        break;
                    case ConsoleKey.RightArrow:
                        acted = MoveHero(Direction.E);
                        break;
                    case ConsoleKey.P:
                        acted = PickUp();
                        break;
                    case ConsoleKey.D:
                        acted = Drop();
                        break;
                    case ConsoleKey.I:
                        acted = Inventory();
                        break;
                    case ConsoleKey.W:
                        acted = Wield();
                        break;
                    case ConsoleKey.U:
                        acted = Use();
                        break;
                    case ConsoleKey.H:
                        acted = Help();
                        break;
                    case ConsoleKey.Q:
                        gameInProgress = false;
                        break;
                }

                Draw();
            } while (gameInProgress && !acted && !hero.IsDead);

            if (hero.IsDead) gameInProgress = false;
        }

        private bool Use()
        {
            var consumables = hero.Backpack.OfType<IConsumable>().ToList();
            var consumable = ui.MenuSelect("What do you want to use?", consumables);
            if (consumable == null) return false;
            return hero.Consume(consumable);
        }

        private bool Help()
        {
            ui.MenuWrite(new List<string>
            {
                "These commands are at your disposal:",
                "",
                "  P) Pick up item",
                "  D) Drop item",
                "  I) Inventory",
                "  W) Wield weapon",
                "  U) Use item",
                "  H) This help",
                "  Q) Quit the game",
                "",
                "Use arrow keys to walk or attack"
            });
            return false;
        }

        private bool Drop()
        {
            var item = ui.MenuSelect("What do you want to drop?", hero.Backpack.ToList());
            if (item == null) return false;

            if (hero.Backpack.Remove(item))
            {
                level.Cell(hero.Position).Items.Add(item);
                log.Add($"You dropped the {item}");
                return true;
            }

            log.Add("You couldn't remove the {item} from your backpack");
            return false;
        }

        private bool Inventory()
        {
            ui.MenuList("Your backpack contains:", hero.Backpack);
            return false;
        }

        private bool Wield()
        {
            var weapons = hero.Backpack.OfType<Weapon>().ToList();
            var selectedWeapon = ui.MenuSelect("Select weapon:", weapons);
            if (selectedWeapon == null) return false;

            if (hero.Weapon != null)
                if (!hero.Backpack.IsFull())
                    hero.Backpack.Add(hero.Weapon);
                else
                    level.Cell(hero.Position).Items.Add(hero.Weapon);

            hero.Backpack.Remove(selectedWeapon);
            hero.Weapon = selectedWeapon;

            return false; // doesn't count as an action
        }


        private bool PickUp()
        {
            var cell = level.Cell(hero.Position);
            var items = cell.Items;
            var count = items.Count;

            Item item;
            switch (count)
            {
                case 0:
                    log.Add("There's nothing here");
                    return false;
                case 1:
                    item = items[index: 0];
                    break;
                default:
                    item = ui.MenuSelect("Pick one:", items);
                    break;
            }

            if (item != null) return hero.PickUp(item);
            return false;
        }

        private bool MoveHero(Position direction)
        {
            return hero.Walk(direction);
        }

        private void Draw()
        {
            ui.ReStart();
            ui.DrawMap();
            ui.DrawStatus(hero);
            ui.WriteLog();
        }

        private void Init()
        {
            hero = new Hero();

            level.PlaceAt(0, 0, hero);
            level.PlaceAt(5, 7, new Goblin());
            level.PlaceAt(7, 5, new Goblin());
            level.PlaceAt(3, 3, new Goblin());
            level.PlaceAt(9, 3, new Orc());
            level.PlaceAt(7, 2, new Orc());
            level.PlaceAt(4, 6, new Troll());
            level.PlaceAt(2, 8, new Troll());
            level.PlaceAt(6, 6, Weapon.Dagger());
            level.PlaceAt(2, 8, Weapon.Sword());
            level.PlaceAt(6, 6, Item.Coin);
            level.PlaceAt(7, 6, Item.Coin);
            level.PlaceAt(9, 9, Item.Gem);
            level.PlaceAt(4, 4, new TelePotion());
            level.PlaceAt(8, 4, new HealthPotion());
            
            level.Cell(5, 4).Environment = Environment.Abyss;
            level.Cell(7, 7).Environment = Environment.Wall;
            level.Cell(7, 8).Environment = Environment.Wall;
            level.Cell(3, 8).Environment = Environment.Water;

            log.Add("Welcome to the Donjon!");
        }
    }
}