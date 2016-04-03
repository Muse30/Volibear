namespace Volibear
{
    using System;
    using System.Linq;

    using EloBuddy;
    using EloBuddy.SDK;
    using EloBuddy.SDK.Enumerations;
    using EloBuddy.SDK.Rendering;
    using EloBuddy.SDK.Events;
    using EloBuddy.SDK.Menu;
    using EloBuddy.SDK.Menu.Values;
    using Color = System.Drawing.Color;

    using SharpDX;
    using System.Drawing;

    internal class Volibear
    {
        public static Spell.Active Q { get; private set; }

        public static Spell.Targeted W { get; private set; }

        public static Spell.Active E { get; private set; }

        public static Spell.Active R { get; private set; }

        public static Spell.Targeted Smitee;

        public static readonly string[] SmiteableUnits =
        {
            "SRed", "SBlue", "SDragon", "SBaron"
        };

        private static readonly int[] SmiteRed = { 3715, 1415, 1414, 1413, 1412 };
        private static readonly int[] SmiteBlue = { 3706, 1403, 1402, 1401, 1400 };

        public static double TotalDamage = 0;

        public static AIHeroClient _player { get; set; }

        public static Menu ComboMenu { get; private set; }

        public static Menu HarassMenu { get; private set; }

        public static Menu LaneMenu { get; private set; }

        public static Menu MiscMenu { get; private set; }

        public static Menu KSMenu { get; private set; }

        public static Menu JungleMenu { get; private set; }

        public static SpellSlot Smite;

        public static Menu DrawMenu { get; private set; }

        private static Menu VoliMenu;

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnLoad;
        }

        private static void OnLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != "Volibear")
            {
                return;
            }

            VoliMenu = MainMenu.AddMenu("Volibear", "Volibear");
            VoliMenu.AddGroupLabel("Crazy Voli!");
            ComboMenu = VoliMenu.AddSubMenu("Combo");
            ComboMenu.AddGroupLabel("Combo Settings");
            ComboMenu.Add("UseQ", new CheckBox("Use Q"));
            ComboMenu.Add("UseW", new CheckBox("Use W"));
            ComboMenu.Add("UseE", new CheckBox("Use E"));
            ComboMenu.Add("UseR", new CheckBox("Use R"));
            ComboMenu.Add("UseItems", new CheckBox("Use Items"));
            ComboMenu.Add("Wcount", new Slider("Enemy health % to use W", 100, 0, 100));
            ComboMenu.Add("Rcount", new Slider("Num of Enemy in Range to Ult", 2, 1, 5));
            ComboMenu.Add("BlueSmite", new CheckBox("KS with Smite"));
            ComboMenu.Add("Redsmite", new CheckBox("Combo with Smite (red)"));

            HarassMenu = VoliMenu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("Harass Settings");
            HarassMenu.Add("Ehrs", new CheckBox("Use E in Harass"));
        

            LaneMenu = VoliMenu.AddSubMenu("Farm");
            LaneMenu.AddGroupLabel("LaneClear Settings");
            LaneMenu.Add("laneQ", new CheckBox("Use Q"));
            LaneMenu.Add("laneW", new CheckBox("Use W"));
            LaneMenu.Add("laneE", new CheckBox("Use E"));
            LaneMenu.Add("LCM", new Slider("Mana %", 30, 0, 100));


            JungleMenu = VoliMenu.AddSubMenu("Jungle");
            JungleMenu.AddGroupLabel("JungleClear Settings");
            JungleMenu.Add("JungleQ", new CheckBox("Use Q"));
            JungleMenu.Add("JungleW", new CheckBox("Use W"));
            JungleMenu.Add("JungleE", new CheckBox("Use E"));
            JungleMenu.Add("JCM", new Slider("Mana %", 30, 0, 100));
            JungleMenu.Add("Sbaron", new CheckBox("Smite Baron"));
            JungleMenu.Add("sDragon", new CheckBox("Smite Dragon"));
            JungleMenu.Add("SBlue", new CheckBox("Smite Blue"));
            JungleMenu.Add("SRed", new CheckBox("Smite Red"));


            MiscMenu = VoliMenu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("Misc Settings");
            MiscMenu.Add("gapcloserW", new CheckBox("Anti-GapCloser W"));
            MiscMenu.Add("smiteActive",
              new KeyBind("Smite Active (toggle)", true, KeyBind.BindTypes.PressToggle, 'H'));

            KSMenu = VoliMenu.AddSubMenu("ks");
            KSMenu.AddGroupLabel("killsteal Settings");
            KSMenu.Add("ksW", new CheckBox("KS with W"));
            KSMenu.Add("ksE", new CheckBox("KS with E"));

            DrawMenu = VoliMenu.AddSubMenu("Drawings");
            DrawMenu.AddGroupLabel("Drawing Settings");
            DrawMenu.Add("DrawWE", new CheckBox("Draw W and E"));
            DrawMenu.Add("smitestatus", new CheckBox("Draw Smite Status"));



            Q = new Spell.Active(SpellSlot.Q, 750);
            W = new Spell.Targeted(SpellSlot.W, 395);
            E = new Spell.Active(SpellSlot.E, 415);
            R = new Spell.Active(SpellSlot.R, (uint)Player.Instance.GetAutoAttackRange());

            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Game.OnUpdate += SmiteEvent;

        }

        private static void OnUpdate(EventArgs args)
        {
            _player = ObjectManager.Player;

          
          {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)) Combo();
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass)) Harass();
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear)) LaneClear();
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear)) JungleClear(); 
                
                }

                KillSteal();
            }


        static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
        if (ComboMenu["UseR"].Cast<CheckBox>().CurrentValue && R.IsReady() && args.Target.IsEnemy &&
            args.Target.IsValid &&
            _player.CountEnemiesInRange(300) >= ComboMenu["Rcount"].Cast<Slider>().CurrentValue)

        {
            R.Cast();
        }
    }


        private static void Drawing_OnDraw(EventArgs args)
        {
            if (DrawMenu["DrawWE"].Cast<CheckBox>().CurrentValue)
            {
                Drawing.DrawCircle(_player.Position, E.Range, Color.CadetBlue);
            }
            var heropos = Drawing.WorldToScreen(ObjectManager.Player.Position);
            var green = Color.LimeGreen;
            var red = Color.IndianRed;
            if (DrawMenu["smitestatus"].Cast<CheckBox>().CurrentValue)
            {
                Drawing.DrawText(heropos.X - 40, heropos.Y + 20, System.Drawing.Color.FloralWhite, "Smite:");
                Drawing.DrawText(heropos.X + 10, heropos.Y + 20,
                    MiscMenu["smiteActive"].Cast<KeyBind>().CurrentValue ? System.Drawing.Color.LimeGreen : System.Drawing.Color.Red,
                    MiscMenu["smiteActive"].Cast<KeyBind>().CurrentValue ? "On" : "Off");

            }
        }

        private static void KillSteal()
        {
            var enemyForKs = EntityManager.Heroes.Enemies.FirstOrDefault(h => W.IsReady() && WDamage(h) > h.Health);
            if (enemyForKs != null && W.IsReady() && KSMenu["ksW"].Cast<CheckBox>().CurrentValue)
            {
                W.Cast(enemyForKs);
            }

            var kSableE =
                EntityManager.Heroes.Enemies.FindAll(
                    champ =>
                        champ.IsValidTarget() &&
                        (champ.Health <= ObjectManager.Player.GetSpellDamage(champ, SpellSlot.E)) &&
                        champ.Distance(ObjectManager.Player) < E.Range);
            if (kSableE.Any())
            {
                E.Cast(kSableE.FirstOrDefault());
            }
        }

        public static double WDamage(Obj_AI_Base target)
        {
            return (new double[] { 80, 125, 170, 215, 260 }[W.Level - 1] +
                    ((_player.MaxHealth - (498.48f + (86f * (_player.Level - 1f)))) * 0.15f)) *
                   ((target.MaxHealth - target.Health) / target.MaxHealth + 1);
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(1490, DamageType.Physical);
            if (target == null || target.IsZombie) return;


            if (_player.Distance(target) <= Q.Range && Q.IsReady() && (ComboMenu["UseQ"].Cast<CheckBox>().CurrentValue))
            {
               Q.Cast();
            }
            if (_player.Distance(target) <= E.Range && E.IsReady() && (ComboMenu["UseE"].Cast<CheckBox>().CurrentValue))
            {
                E.Cast();
            }

            var T = TargetSelector.GetTarget(W.Range, DamageType.Physical);
            var health = target.Health;
            var maxhealth = target.MaxHealth;
            float wcount = ComboMenu["Wcount"].Cast<Slider>().CurrentValue;
            if (health < ((maxhealth * wcount) / 100))
            {
                if (ComboMenu["UseW"].Cast<CheckBox>().CurrentValue && W.IsReady())
                {
                    W.Cast(T);
                }

                if (ComboMenu["UseItems"].Cast<CheckBox>().CurrentValue)
                {
                    UseItems(target);
                }
            }
        }

        private static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            var T = TargetSelector.GetTarget(W.Range, DamageType.Physical);
            if (_player.IsDead || !sender.IsEnemy || !sender.IsValidTarget(W.Range) || !W.IsReady() || !MiscMenu["gapcloserW"].Cast<CheckBox>().CurrentValue) return;

            W.Cast(T);
        }



        internal static void UseItems(Obj_AI_Base target)
        {
            var KhazixServerPosition = _player.ServerPosition.To2D();
            var targetServerPosition = target.ServerPosition.To2D();

            if (Item.CanUseItem(ItemId.Ravenous_Hydra_Melee_Only) && 400 > _player.Distance(target))
            {
                Item.UseItem(ItemId.Ravenous_Hydra_Melee_Only);
            }
            if (Item.CanUseItem(ItemId.Tiamat_Melee_Only) && 400 > _player.Distance(target))
            {
                Item.UseItem(ItemId.Tiamat_Melee_Only);
            }
            if (Item.CanUseItem(ItemId.Titanic_Hydra) && 400 > _player.Distance(target))
            {
                Item.UseItem(ItemId.Titanic_Hydra);
            }
            if (Item.CanUseItem(ItemId.Blade_of_the_Ruined_King) && 550 > _player.Distance(target))
            {
                Item.UseItem(ItemId.Blade_of_the_Ruined_King);
            }
            if (Item.CanUseItem(ItemId.Youmuus_Ghostblade) && _player.GetAutoAttackRange() > _player.Distance(target))
            {
                Item.UseItem(ItemId.Youmuus_Ghostblade);
            }
            if (Item.CanUseItem(ItemId.Bilgewater_Cutlass) && 550 > _player.Distance(target))
            {
                Item.UseItem(ItemId.Bilgewater_Cutlass);
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);

            if (target == null)
            {
                return;
            }

            if (HarassMenu["Ehrs"].Cast<CheckBox>().CurrentValue && _player.Distance(target) <= E.Range && E.IsReady())
            {
                E.Cast();
            }
        }

        private static void JungleClear()
        {
            var Mob = EntityManager.MinionsAndMonsters.GetJungleMonsters(_player.ServerPosition, E.Range).OrderBy(x => x.MaxHealth).ToList();

            if (_player.ManaPercent > JungleMenu["JCM"].Cast<Slider>().CurrentValue)
            {
                if (JungleMenu["JungleQ"].Cast<CheckBox>().CurrentValue && Q.IsReady())
                {
                    foreach (var minion in Mob)
                    {
                        if (minion.IsValidTarget())
                        {
                            Q.Cast();
                        }
                    }
                }

                if (JungleMenu["JungleW"].Cast<CheckBox>().CurrentValue && W.IsReady())
                {
                    foreach (var minion in Mob)
                    {
                        if (minion.IsValidTarget())
                        {
                            W.Cast(minion);
                        }
                    }
                }
                if (JungleMenu["JungleE"].Cast<CheckBox>().CurrentValue && E.IsReady())
                {
                    foreach (var minion in Mob)
                    {
                        if (minion.IsValidTarget())
                        {
                            E.Cast();
                        }
                    }
                }
            }
        }

        public static void SetSmiteSlot()
        {
            SpellSlot smiteSlot;
            if (SmiteBlue.Any(x => _player.InventoryItems.FirstOrDefault(a => a.Id == (ItemId)x) != null))
                smiteSlot = _player.GetSpellSlotFromName("s5_summonersmiteplayerganker");
            else if (
                SmiteRed.Any(
                    x => _player.InventoryItems.FirstOrDefault(a => a.Id == (ItemId)x) != null))
                smiteSlot = _player.GetSpellSlotFromName("s5_summonersmiteduel");
            else
                smiteSlot = _player.GetSpellSlotFromName("summonersmite");
            Smitee = new Spell.Targeted(smiteSlot, 500);
        }


        public static int GetSmiteDamage()
        {
            var level = _player.Level;
            int[] smitedamage =
            {
                20*level + 370,
                30*level + 330,
                40*level + 240,
                50*level + 100
            };
            return smitedamage.Max();
        }

        private static void SmiteEvent(EventArgs args)
        {
            SetSmiteSlot();
            if (!Smitee.IsReady() || _player.IsDead) return;
            if (MiscMenu["smiteActive"].Cast<KeyBind>().CurrentValue)
            {
                var unit =
                    EntityManager.MinionsAndMonsters.Monsters
                        .Where(
                            a =>
                                SmiteableUnits.Contains(a.BaseSkinName) && a.Health < GetSmiteDamage() &&
                                JungleMenu[a.BaseSkinName].Cast<CheckBox>().CurrentValue)
                        .OrderByDescending(a => a.MaxHealth)
                        .FirstOrDefault();

                if (unit != null)
                {
                    Smitee.Cast(unit);
                    return;
                }
            }
            if (ComboMenu["BlueSmite"].Cast<CheckBox>().CurrentValue &&
                Smitee.Handle.Name == "s5_summonersmiteplayerganker")
            {
                foreach (
                    var target in
                        EntityManager.Heroes.Enemies
                            .Where(h => h.IsValidTarget(Smitee.Range) && h.Health <= 20 + 8 * _player.Level))
                {
                    Smitee.Cast(target);
                    return;
                }
            }
            if (ComboMenu["Redsmite"].Cast<CheckBox>().CurrentValue &&
                Smitee.Handle.Name == "s5_summonersmiteduel" &&
                Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                foreach (
                    var target in
                        EntityManager.Heroes.Enemies
                            .Where(h => h.IsValidTarget(Smitee.Range)).OrderByDescending(TargetSelector.GetPriority)
                    )
                {
                    Smitee.Cast(target);
                    return;
                }
            }
        }


        private static void LaneClear()
        {
            var allMinions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, _player.ServerPosition, Q.Range);

            if (_player.ManaPercent > LaneMenu["LCM"].Cast<Slider>().CurrentValue)
            {
                if (LaneMenu["LaneW"].Cast<CheckBox>().CurrentValue && Q.IsReady())
                {
                    foreach (var minion in allMinions)
                    {
                        if (minion.IsValidTarget())
                        {
                            W.Cast(minion);
                        }
                    }
                }

                if (LaneMenu["LaneE"].Cast<CheckBox>().CurrentValue && E.IsReady())
                {
                    foreach (var minion in allMinions)
                    {
                        if (minion.IsValidTarget())
                        {
                            E.Cast();
                        }
                    }
                }
            }
        }
    }
}