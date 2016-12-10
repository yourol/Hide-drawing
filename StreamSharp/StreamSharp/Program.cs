// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Nouser">
// 
// Copyright 2014 - 2015 LeagueSharp
// StreamSharp uses LeagueSharp.Common.
// 
// LeagueSharp.Common is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// LeagueSharp.Common is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with LeagueSharp.Common. If not, see <http://www.gnu.org/licenses/>.
// </copyright>
// <summary>
//   A streaming assembly for Leaguesharp
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace StreamSharp
{
    using System;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;

    /// <summary>
    /// The program.
    /// </summary>
    internal class Program
    {
        #region Static Fields

        /// <summary>
        /// If the user is attacking
        /// Currently used for the second style of fake clicks
        /// </summary>
        private static bool attacking = false;

        /// <summary>
        /// The delta t for click frequency
        /// </summary>
        private static float deltaT = .2f;

        /// <summary>
        /// The last direction of the player
        /// </summary>
        private static Vector3 direction;

        /// <summary>
        /// The last endpoint the player was moving to.
        /// </summary>
        private static Vector3 lastEndpoint = new Vector3();

        /// <summary>
        /// The last order the player had.
        /// </summary>
        private static GameObjectOrder lastOrder;

        /// <summary>
        /// The time of the last order the player had.
        /// </summary>
        private static float lastOrderTime = 0f;

        /// <summary>
        /// The last time a click was done.
        /// </summary>
        private static float lastTime = 0f;

        /// <summary>
        /// The Player.
        /// </summary>
        private static Obj_AI_Hero player;

        /// <summary>
        /// The Random number generator
        /// </summary>
        private static Random r = new Random();

        /// <summary>
        /// The root menu.
        /// </summary>
        private static Menu root = new Menu("Stream", "Stream", true);

        #endregion

        #region Methods

        /// <summary>
        /// The move fake click after attacking
        /// </summary>
        /// <param name="unit">
        /// The unit.
        /// </param>
        /// <param name="target">
        /// The target.
        /// </param>
        private static void AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            attacking = false;
            var t = target as Obj_AI_Hero;
            if (t != null && unit.IsMe)
            {
                Hud.ShowClick(ClickType.Move, RandomizePosition(t.Position));
            }
        }

        /// <summary>
        /// The angle between two vectors.
        /// </summary>
        /// <param name="a">
        /// The first vector.
        /// </param>
        /// <param name="b">
        /// The second vector.
        /// </param>
        /// <returns>
        /// The Angle between two vectors
        /// </returns>
        private static float AngleBetween(Vector3 a, Vector3 b)
        {
            var dotProd = Vector3.Dot(a, b);
            var lenProd = a.Length() * b.Length();
            var divOperation = dotProd / lenProd;
            return (float)(Math.Acos(divOperation) * (180.0 / Math.PI));
        }

        /// <summary>
        /// The before attack fake click.
        /// Currently used for the second style of fake clicks
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void BeforeAttackFake(Orbwalking.BeforeAttackEventArgs args)
        {
            if (root.SubMenu("Fake Clicks").Item("Click Mode").GetValue<StringList>().SelectedIndex == 1)
            {
                Hud.ShowClick(ClickType.Attack, RandomizePosition(args.Target.Position));
                attacking = true;
            }
        }

        /// <summary>
        /// The fake click before you cast a spell
        /// </summary>
        /// <param name="s">
        /// The Spell Book.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void BeforeSpellCast(Spellbook s, SpellbookCastSpellEventArgs args)
        {
            if (args.Target.Position.Distance(player.Position) >= 5f)
            {
                Hud.ShowClick(ClickType.Attack, args.Target.Position);
            }
        }

        /// <summary>
        /// The on new path fake.
        /// Currently used for the second style of fake clicks
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void DrawFake(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
        {
            if (sender.IsMe && lastTime + deltaT < Game.Time && args.Path.LastOrDefault() != lastEndpoint
                && args.Path.LastOrDefault().Distance(player.ServerPosition) >= 5f
                && root.SubMenu("Fake Clicks").Item("Enable").IsActive()
                && root.SubMenu("Fake Clicks").Item("Click Mode").GetValue<StringList>().SelectedIndex == 1)
            {
                lastEndpoint = args.Path.LastOrDefault();
                if (!attacking)
                {
                    Hud.ShowClick(ClickType.Move, Game.CursorPos);
                }
                else
                {
                    Hud.ShowClick(ClickType.Attack, Game.CursorPos);
                }

                lastTime = Game.Time;
            }
        }

        /// <summary>
        /// The main entry point.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnLoad;
            Game.OnUpdate += OnUpdate;
            Hacks.DisableDrawings = true;
            Hacks.DisableSay = true;
            Hacks.PingHack = false;
        }

        /// <summary>
        /// The OnIssueOrder event delegate.
        /// Currently used for the first style of fake clicks
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void OnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            if (sender.IsMe
                && (args.Order == GameObjectOrder.MoveTo || args.Order == GameObjectOrder.AttackUnit
                    || args.Order == GameObjectOrder.AttackTo)
                && lastOrderTime + r.NextFloat(deltaT, deltaT + .2f) < Game.Time
                && root.SubMenu("Fake Clicks").Item("Enable").IsActive()
                && root.SubMenu("Fake Clicks").Item("Click Mode").GetValue<StringList>().SelectedIndex == 0)
            {
                var vect = args.TargetPosition;
                vect.Z = player.Position.Z;
                if (args.Order == GameObjectOrder.AttackUnit || args.Order == GameObjectOrder.AttackTo)
                {
                    Hud.ShowClick(ClickType.Attack, RandomizePosition(vect));
                }
                else
                {
                    Hud.ShowClick(ClickType.Move, vect);
                }

                lastOrderTime = Game.Time;
            }
        }

        /// <summary>
        /// The OnLoad event delegate
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void OnLoad(EventArgs args)
        {
            root.AddItem(new MenuItem("Drawings", "Drawings").SetValue(new KeyBind('H', KeyBindType.Toggle, true)));
            root.AddItem(new MenuItem("Game Say", "Game Say").SetValue(new KeyBind('K', KeyBindType.Toggle, false)));
            root.AddItem(new MenuItem("Stream", "Stream").SetValue(new KeyBind('L', KeyBindType.Toggle, false)));
            root.AddItem(new MenuItem("Config", "Config").SetValue(new KeyBind(';', KeyBindType.Toggle, false)));

            var fakeClickMenu = new Menu("Fake Clicks", "Fake Clicks", false);
            fakeClickMenu.AddItem(new MenuItem("Enable", "Enable").SetValue(true));
            fakeClickMenu.AddItem(new MenuItem("Click Mode", "Click Mode"))
                .SetValue(new StringList(new[] { "Evade, No Cursor Position", "Cursor Position, No Evade" }));
            root.AddSubMenu(fakeClickMenu);
            root.AddToMainMenu();

            player = ObjectManager.Player;

            Obj_AI_Base.OnNewPath += DrawFake;
            Orbwalking.BeforeAttack += BeforeAttackFake;
            Spellbook.OnCastSpell += BeforeSpellCast;
            Orbwalking.AfterAttack += AfterAttack;
            Obj_AI_Base.OnIssueOrder += OnIssueOrder;
        }

        /// <summary>
        /// The OnUpdate event delegate.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void OnUpdate(EventArgs args)
        {
            if (!root.Item("Stream").IsActive() && !root.Item("Config").IsActive())
            {
                Hacks.DisableDrawings = !root.Item("Drawings").IsActive();
                Hacks.DisableSay = root.Item("Game Say").IsActive();
            }
            else if (root.Item("Config").IsActive())
            {
                Hacks.DisableDrawings = false;
                Hacks.DisableSay = false;
            }
            else
            {
                Hacks.DisableDrawings = true;
                Hacks.DisableSay = true;
            }
        }

        /// <summary>
        /// The RandomizePosition function to randomize click location.
        /// </summary>
        /// <param name="input">
        /// The input Vector3.
        /// </param>
        /// <returns>
        /// A Vector within 100 units of the unit
        /// </returns>
        private static Vector3 RandomizePosition(Vector3 input)
        {
            if (r.Next(2) == 0)
            {
                input.X += r.Next(100);
            }
            else
            {
                input.Y += r.Next(100);
            }

            return input;
        }

        #endregion
    }
}