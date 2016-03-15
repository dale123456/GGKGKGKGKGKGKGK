using System;
using System.Linq;
using System.Collections.Generic;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using Color = System.Drawing.Color;
using EloBuddy.SDK.Constants;
using SharpDX;

namespace Blessed_Riven
{
    class Program
    {
        public static Spell.Active Q = new Spell.Active(SpellSlot.Q, 300);
        public static Spell.Active E = new Spell.Active(SpellSlot.E, 325);
        public static Spell.Skillshot R = new Spell.Skillshot(SpellSlot.R, 900, SkillShotType.Cone, 250, 1600, 45)
        {
            AllowedCollisionCount = int.MaxValue
        };
        public static Spell.Active W
        {
            get
            {
                return new Spell.Active(SpellSlot.W,
                    (uint)
                        (70 + Player.Instance.BoundingRadius +
                         (Player.Instance.HasBuff("RivenFengShuiEngine") ? 195 : 120)));
            }
        }
        static Spell.Targeted Smite = null;
        public static bool EnableR;
        public static int LastCastQ;
        public static int LastCastW;
        private static int lastwd;
        private static readonly float _barLength = 104;
        private static readonly float _xOffset = 2;
        private static readonly float _yOffset = 9;
        private static bool ssfl;
        public static int QCount;
        public static Menu Menu, FarmingMenu, MiscMenu, DrawMenu, HarassMenu, ComboMenu, Skin, DelayMenu,SmiteMenu;
        static Item Healthpot;
        public static SpellSlot SmiteSlot = SpellSlot.Unknown;
        public static SpellSlot IgniteSlot = SpellSlot.Unknown;
        private static readonly int[] SmitePurple = { 3713, 3726, 3725, 3726, 3723 };
        private static readonly int[] SmiteGrey = { 3711, 3722, 3721, 3720, 3719 };
        private static readonly int[] SmiteRed = { 3715, 3718, 3717, 3716, 3714 };
        private static readonly int[] SmiteBlue = { 3706, 3710, 3709, 3708, 3707 };
        private static Spell.Targeted _ignite;

        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;

        }
        public static AIHeroClient _Player
        {
            get { return ObjectManager.Player; }


        }
        private static string Smitetype
        {
            get
            {
                if (SmiteBlue.Any(i => Item.HasItem(i)))
                    return "s5_summonersmiteplayerganker";

                if (SmiteRed.Any(i => Item.HasItem(i)))
                    return "s5_summonersmiteduel";

                if (SmiteGrey.Any(i => Item.HasItem(i)))
                    return "s5_summonersmitequick";

                if (SmitePurple.Any(i => Item.HasItem(i)))
                    return "itemsmiteaoe";

                return "summonersmite";
            }
        }
        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Player.Instance.ChampionName != "리븐") return;


            SpellDataInst smite = _Player.Spellbook.Spells.Where(spell => spell.Name.Contains("강타")).Any() ? _Player.Spellbook.Spells.Where(spell => spell.Name.Contains("강타")).First() : null;
            if (smite != null)
            {
                Smite = new Spell.Targeted(smite.Slot, 500);
            }
            Healthpot = new Item(2003, 0);
            _ignite = new Spell.Targeted(ObjectManager.Player.GetSpellSlotFromName("summonerdot"), 600);

            Chat.Print("태범아 원하는 리븐핵이다 시발.", Color.white);
            Menu = MainMenu.AddMenu("GangKimRiven", "GangKimRiven");

            ComboMenu = Menu.AddSubMenu("콤보 세팅", "콤보 세팅");
            ComboMenu.AddLabel("Combo Settings");
            ComboMenu.Add("Q콤보", new CheckBox("Q사용"));
            ComboMenu.Add("W콤보", new CheckBox("w사용"));
            ComboMenu.Add("E콤보", new CheckBox("e사용"));
            ComboMenu.Add("R콤보", new CheckBox("r사용"));
            ComboMenu.Add("R2콤보", new CheckBox("R2사용"(킬각때 사용)"));
            ComboMenu.Add("플W", new KeyBind("Flash W", faelse, KeyBind.BindTypes.HoldActive, '5'));
            ComboMenu.Add("플래시Burst모드", new KeyBind("Burst(현재오류)", false, KeyBind.BindTypes.HoldActive, 'G'));
            ComboMenu.AddLabel("Burst = 타겟을 누른후 키누르면 burst모드");
            ComboMenu.AddLabel("The flash has usesh");
            ComboMenu.AddLabel("If not perform without a flash");
            ComboMenu.Add("강제R", new KeyBind("강제 R", true, KeyBind.BindTypes.PressToggle, 'Z'));
            ComboMenu.Add("useTiamat", new CheckBox("(아이템사용)");
            ComboMenu.AddLabel("R 세팅");
            ComboMenu.Add("RCantKill", new CheckBox("Cant Kill with Combo", false));
            ComboMenu.Add("R적수에따른발동t", new Slider("적 수 >= ", 0, 0, 4));

            HarassMenu = Menu.AddSubMenu("평타,견제세팅", "평타견제세팅");
            HarassMenu.AddLabel("평타 견제세팅");
            HarassMenu.Add("QHarass", new CheckBox("Use Q"));
            HarassMenu.Add("WHarass", new CheckBox("Use W"));
            HarassMenu.Add("EHarass", new CheckBox("Use E"));
            var Style = HarassMenu.Add("harassstyle", new Slider("평타견제 스타일", 0, 0, 2));
            Style.OnValueChange += delegate
            {
                Style.DisplayName = "콤보스타일: " + new[] { "Q,Q,W,Q 그리고 E 빼기", "E,H,Q3,W", "E,H,AA,Q,W" }[Style.CurrentValue];
            };
            Style.DisplayName = "콤보스타일: " + new[] { "Q,Q,W,Q 그리고 E 빼기", "E,H,Q3,W", "E,H,AA,Q,W" }[Style.CurrentValue];

            FarmingMenu = Menu.AddSubMenu("라인클리어 세팅", "정글세팅");
            FarmingMenu.AddLabel("라인클리어");
            FarmingMenu.Add("Q라인클리어", new CheckBox("Q로 라인클리어"));
            FarmingMenu.Add("W라인클리어", new CheckBox("W로 라인클리어"));
            FarmingMenu.Add("E라인클리어", new CheckBox("E로 라인클리어"));

            FarmingMenu.AddLabel("정글 클리어");
            FarmingMenu.Add("QJungleClear", new CheckBox("정글에서 q사용"));
            FarmingMenu.Add("WJungleClear", new CheckBox("정글에서 w사용"));
            FarmingMenu.Add("EJungleClear", new CheckBox("정글에서 e사용"));

            FarmingMenu.AddLabel("막타");
            FarmingMenu.Add("Qlasthit", new CheckBox("막타 q"));
            FarmingMenu.Add("Wlasthit", new CheckBox("막타 w"));
            FarmingMenu.Add("Elasthit", new CheckBox("막타 e"));

            SetSmiteSlot();
            if (SmiteSlot != SpellSlot.Unknown)
            {
                SmiteMenu = Menu.AddSubMenu("강타", "강타");
                SmiteMenu.Add("강타콤보", new CheckBox("적있을때 강타 콤보"));
                SmiteMenu.AddLabel("막타강타");
                SmiteMenu.Add("Use Smite?", new CheckBox("자동막타강타"));
                SmiteMenu.Add("레드?", new CheckBox("레드"));
                SmiteMenu.Add("레드?", new CheckBox("블루"));
                SmiteMenu.Add("용?", new CheckBox("용"));
                SmiteMenu.Add("바론?", new CheckBox("바론"));
            }

            MiscMenu = Menu.AddSubMenu("더많은 세팅", "Misc");
            MiscMenu.AddLabel("자동");
            MiscMenu.Add("쉴드 사용", new CheckBox("쉴드사용(E)"));
            MiscMenu.Add("자동 점화", new CheckBox("자동 점화"));
            MiscMenu.Add("자동QSS", new CheckBox("자동 Q평캔"));
            MiscMenu.Add("자동W", new CheckBox("자동 W"));
            MiscMenu.AddLabel("Keep Alive Settings");
            MiscMenu.Add("Alive.Q", new CheckBox("Keep Q Alive"));
            MiscMenu.Add("Alive.R", new CheckBox("Use R2 Before Expire"));
            MiscMenu.AddLabel("Extra");
            MiscMenu.Add("interrupter", new CheckBox("Use Interruptable Spells"));
            MiscMenu.Add("gapcloser", new CheckBox("Use Gapclose Spells"));
            MiscMenu.AddLabel("자동 아이템 사용");
            MiscMenu.Add("useHP", new CheckBox("체력포션 사용"));
            MiscMenu.Add("useHPV", new Slider("HP < %", 45, 0, 100));
            MiscMenu.Add("useElixir", new CheckBox("Use Elixir"));
            MiscMenu.Add("useElixirCount", new Slider("EnemyCount > ", 1, 0, 4));
            MiscMenu.Add("useCrystal", new CheckBox("Use Refillable Potions"));
            MiscMenu.Add("useCrystalHPV", new Slider("HP < %", 65, 0, 100));
            MiscMenu.Add("useCrystalManaV", new Slider("Mana < %", 65, 0, 100));

            DelayMenu = Menu.AddSubMenu("Delay Settings(인간답게)", "딜레이");
            DelayMenu.Add("인간답게하는거", new CheckBox("헬퍼티안나게 하는거사용?", false));
            DelayMenu.Add("spell1a1b", new Slider("Q1,Q2 딜레이(ms)", 261, 100, 400));
            DelayMenu.Add("spell1c", new Slider("Q3 딜레이(ms)", 353, 100, 400));
            DelayMenu.Add("spell2", new Slider("W 딜레이(ms)", 120, 100, 400));
            DelayMenu.Add("spell4a", new Slider("R 딜레이(ms)", 0, 0, 400));
            DelayMenu.Add("spell4b", new Slider("R2 딜레이(ms)", 100, 50, 400));

            Skin = Menu.AddSubMenu("스킨체인저", "스킨체인저");
            Skin.Add("스킨체크", new CheckBox("스킨체인저 사용"));
            Skin.Add("스킨.Id", new Slider("스킨", 4, 0, 6));

            DrawMenu = Menu.AddSubMenu("Draw 세팅", "Drawings");
            DrawMenu.Add("drawStatus", new CheckBox("Draw Status"));
            DrawMenu.Add("drawCombo", new CheckBox(" 콤보 사거리보여주기"));
            DrawMenu.Add("drawFBurst", new CheckBox("플 궁 버스터모드사거리 보여주기"));
            DrawMenu.Add("DrawDamage", new CheckBox("사거리 데미지바 보여주기 "));

            Game.OnTick += Game_OnTick;
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Obj_AI_Base.OnSpellCast += Obj_AI_Base_OnSpellCast;
            Obj_AI_Base.OnPlayAnimation += Obj_AI_Base_OnPlayAnimation;
            Gapcloser.OnGapcloser += Gapcloser_OnGapCloser;
            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
        }

        private static void SetSmiteSlot()
        {
            foreach (
                var spell in
                    _Player.Spellbook.Spells.Where(
                        spell => string.Equals(spell.Name, Smitetype, StringComparison.CurrentCultureIgnoreCase)))
            {
                SmiteSlot = spell.Slot;
            }
        }

        private static void DoQSS()
        {
            if (!MiscMenu["자동QSS"].Cast<CheckBox>().CurrentValue) return;

            if (Item.HasItem(3139) && Item.CanUseItem(3139) && ObjectManager.Player.CountEnemiesInRange(1800) > 0)
            {
                Core.DelayAction(() => Item.UseItem(3139), 1);
            }

            if (Item.HasItem(3140) && Item.CanUseItem(3140) && ObjectManager.Player.CountEnemiesInRange(1800) > 0)
            {
                Core.DelayAction(() => Item.UseItem(3140), 1);
            }
        }
        
        private static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender,
            Interrupter.InterruptableSpellEventArgs e)
        {
            
                if (MiscMenu["interrupter"].Cast<CheckBox>().CurrentValue && sender.IsEnemy &&
                    e.DangerLevel >= DangerLevel.Medium && sender.IsValidTarget(900))
                {
                Player.CastSpell(SpellSlot.E, Game.CursorPos);
            }
               
        }
        
        
        public static void Gapcloser_OnGapCloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {             
                if (MiscMenu["gapcloser"].Cast<CheckBox>().CurrentValue && sender.IsEnemy &&
                    sender.IsValidTarget(900))
                {
                Player.CastSpell(SpellSlot.E, Game.CursorPos);
            }          
        }
        
        private static void Game_OnTick(EventArgs args)
        {
            var HPpot = MiscMenu["useHP"].Cast<CheckBox>().CurrentValue;
            var HPv = MiscMenu["useHPv"].Cast<Slider>().CurrentValue;
            var t = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

            if (LastCastQ + 3600 < Environment.TickCount)
            {
                QCount = 0;
            }
            if (MiscMenu["Alive.Q"].Cast<CheckBox>().CurrentValue && !Player.Instance.IsRecalling() && QCount < 3 && QCount > 0 && LastCastQ + 3480 < Environment.TickCount && Player.Instance.HasBuff("RivenTriCleaveBuff") && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Player.CastSpell(SpellSlot.Q,
                    Orbwalker.LastTarget != null && Orbwalker.LastAutoAttack - Environment.TickCount < 3000
                        ? Orbwalker.LastTarget.Position
                        : Game.CursorPos);
                return;
            }
            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies)
            {     
                if (HPpot && Player.Instance.HealthPercent < HPv && _Player.Distance(enemy) < 2000)
                {
                    if (Item.HasItem(Healthpot.Id) && Item.CanUseItem(Healthpot.Id) && !Player.HasBuff("RegenerationPotion"))
                    {
                        Healthpot.Cast();
                    }
                }
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
                SmiteOnTarget(t);
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                Harass();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                LaneClear();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {
                LastHit();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                JungleClear();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
            {
                Flee();
            }
            Auto();
            Smitecast();
        }

        private static void Smitecast()
        {
            if (Smite != null)
            {
                if (Smite.IsReady() && SmiteMenu["자동강타?"].Cast<CheckBox>().CurrentValue)
                {
                    Obj_AI_Minion Mob = EntityManager.MinionsAndMonsters.GetJungleMonsters(_Player.Position, Smite.Range).FirstOrDefault();

                    if (Mob != default(Obj_AI_Minion))
                    {
                        bool kill = Damage.GetSmiteDamage() >= Mob.Health;

                        if (kill)
                        {
                            if ((Mob.Name.Contains("SRU_Dragon") || Mob.Name.Contains("SRU_Baron"))) Smite.Cast(Mob);
                            else if (Mob.Name.StartsWith("SRU_Red") && SmiteMenu["Red?"].Cast<CheckBox>().CurrentValue) Smite.Cast(Mob);
                            else if (Mob.Name.StartsWith("SRU_Blue") && SmiteMenu["Blue?"].Cast<CheckBox>().CurrentValue) Smite.Cast(Mob);
                        }
                    }
                }
            }
        }

        private static void Auto()
        {
            var w = TargetSelector.GetTarget(W.Range, DamageType.Physical);
            if (w.IsValidTarget(W.Range) && MiscMenu["자동W"].Cast<CheckBox>().CurrentValue)
            {
                W.Cast();
            }
            if (_Player.HasBuffOfType(BuffType.Stun) || _Player.HasBuffOfType(BuffType.Taunt) || _Player.HasBuffOfType(BuffType.Polymorph) || _Player.HasBuffOfType(BuffType.Frenzy) || _Player.HasBuffOfType(BuffType.Fear) || _Player.HasBuffOfType(BuffType.Snare) || _Player.HasBuffOfType(BuffType.Suppression))
            {
                DoQSS();
            }
            if (MiscMenu["자동점화"].Cast<CheckBox>().CurrentValue)
            {
                if (!_ignite.IsReady() || Player.Instance.IsDead) return;
                foreach (
                    var source in
                        EntityManager.Heroes.Enemies
                            .Where(
                                a => a.IsValidTarget(_ignite.Range) &&
                                    a.Health < 50 + 20 * Player.Instance.Level - (a.HPRegenRate / 5 * 3)))
                {
                    _ignite.Cast(source);
                    return;
                }
            }
            if (_Player.SkinId != Skin["skin.Id"].Cast<Slider>().CurrentValue)
            {
                if (checkSkin())
                {
                    Player.SetSkinId(SkinId());
                }
            }
        }
        private static void SmiteOnTarget(AIHeroClient t)
        {
            var range = 700f;
            var use = SmiteMenu["적있을때 강타"].Cast<CheckBox>().CurrentValue;
            var itemCheck = SmiteBlue.Any(i => Item.HasItem(i)) || SmiteRed.Any(i => Item.HasItem(i));
            if (itemCheck && use &&
                _Player.Spellbook.CanUseSpell(SmiteSlot) == SpellState.Ready &&
                t.Distance(_Player.Position) < range)
            {
                _Player.Spellbook.CastSpell(SmiteSlot, t);
            }
        }   
        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;

            if (args.SData.Name.ToLower().Contains(W.Name.ToLower()))
            {
                LastCastW = Environment.TickCount;
                return;
            }
            if (args.Target is Obj_AI_Turret || args.Target is Obj_Barracks || args.Target is Obj_BarracksDampener ||
                args.Target is Obj_Building)
                if (args.Target.IsValid && args.Target != null && Q.IsReady() && FarmingMenu["QLaneClear"].Cast<CheckBox>().CurrentValue &&
                    Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                    Player.CastSpell(SpellSlot.Q, (Obj_AI_Base)args.Target);
            AIHeroClient client = args.Target as AIHeroClient;
            if (client != null)
            {
                var target = client;
                if (!target.IsValidTarget()) return;
                if (ComboMenu["점멸Burst"].Cast<CheckBox>().CurrentValue)
                {
                    Core.DelayAction(ForceItem, 50);
                    
                    if (R.IsReady() && R.Name == "rivenizunablade")
                    {
                        ssfl = false;
                        Core.DelayAction(ForceItem, 50);
                        R.Cast(target);
                    }
                    else if (Q.IsReady())
                    {
                        Core.DelayAction(ForceItem, 50);
                        Player.CastSpell(SpellSlot.Q, target.Position);
                    }
                    return;
                }
            }
            if (args.SData.Name.ToLower().Contains(Q.Name.ToLower()))
            {
                LastCastQ = Environment.TickCount;
                if (!MiscMenu["Alive.Q"].Cast<CheckBox>().CurrentValue) return;
                Core.DelayAction(() =>
                {
                    if (!Player.Instance.IsRecalling() && QCount <= 2)
                    {
                        Player.CastSpell(SpellSlot.Q,
                            Orbwalker.LastTarget != null && Orbwalker.LastAutoAttack - Environment.TickCount < 3000
                                ? Orbwalker.LastTarget.Position
                                : Game.CursorPos);
                    }
                }, 3480);
                return;
            }
        }

        private static void ForceItem()
        {
            if (Item.HasItem(3074) && Item.CanUseItem(3074))
            {
                Item.UseItem(3074);
                return;
            }
            else if (Item.HasItem(3077) && Item.CanUseItem(3077))
            {
                Item.UseItem(3077);
                return;
            }
        }

        private static void Obj_AI_Base_OnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (!sender.IsMe) return;
            var t = 0;
            switch (args.Animation)
            {
                case "Spell1a":
                    if (DelayMenu["인간처럼사용"].Cast<CheckBox>().CurrentValue)
                    {
                        t = DelayMenu["spell1a1b"].Cast<Slider>().CurrentValue;
                        QCount = 1;
                    }
                    else
                    {
                        t = 221;
                        QCount = 1;
                    }                                
                    break;
                case "Spell1b":
                    if (DelayMenu["인간답게하는거사용"].Cast<CheckBox>().CurrentValue)
                    {
                        t = DelayMenu["spell1a1b"].Cast<Slider>().CurrentValue;
                        QCount = 2;
                    }
                    else
                    {
                        t = 221;
                        QCount = 2;
                    }
                    break;
                case "Spell1c":
                    if (DelayMenu["인간답게사용"].Cast<CheckBox>().CurrentValue)
                    {
                        t = DelayMenu["spell1c"].Cast<Slider>().CurrentValue;
                        QCount = 0;
                    }
                    else
                    {
                        t = 303;
                        QCount = 0;
                    }
                    break;
                case "Spell2":
                    if (DelayMenu["인간답게사용"].Cast<CheckBox>().CurrentValue)
                    {
                        t = DelayMenu["spell2"].Cast<Slider>().CurrentValue;
                    }
                    else
                    {
                        t = 110;
                    }
                    break;
                case "Spell3":
                    break;
                case "Spell4a":
                    if (DelayMenu["인간답게사용"].Cast<CheckBox>().CurrentValue)
                    {
                        t = DelayMenu["spell4a"].Cast<Slider>().CurrentValue;
                    }
                    else
                    {
                        t = 0;
                    }
                    break;
                case "Spell4b":
                    if (DelayMenu["useHumanizer"].Cast<CheckBox>().CurrentValue)
                    {
                        t = DelayMenu["spell4b"].Cast<Slider>().CurrentValue;
                    }
                    else
                    {
                        t = 100;
                    }
                    break;
            }
            if (t != 0 && ((Orbwalker.ActiveModesFlags != Orbwalker.ActiveModes.None) || ComboMenu["FlashBurst"].Cast<KeyBind>().CurrentValue))
            {
                Orbwalker.ResetAutoAttack();
                Core.DelayAction(CancelAnimation, t - Game.Ping);
            }
        }

        private static void CancelAnimation()
        {
            Player.DoEmote(Emote.Dance);
            Orbwalker.ResetAutoAttack();
        }

        private static void Obj_AI_Base_OnSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            var target = args.Target as Obj_AI_Base;

            // Hydra
            if (args.SData.Name.ToLower().Contains("itemtiamatcleave"))
            {
                Orbwalker.ResetAutoAttack();
                if (W.IsReady())
                {
                    var target2 = TargetSelector.GetTarget(W.Range, DamageType.Physical);
                    if (target2 != null || Orbwalker.ActiveModesFlags != Orbwalker.ActiveModes.None)
                    {
                        Player.CastSpell(SpellSlot.W);
                    }
                }
                return;
            }

            //W
            if (args.SData.Name.ToLower().Contains(W.Name.ToLower()))
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    if (Player.Instance.HasBuff("RivenFengShuiEngine") && R.IsReady() &&
                        ComboMenu["R2콤보"].Cast<CheckBox>().CurrentValue)
                    {
                        ssfl = true;
                        var target2 = TargetSelector.GetTarget(R.Range, DamageType.Physical);
                        if (target2 != null &&
                            (target2.Distance(Player.Instance) < W.Range &&
                             target2.Health >
                             Player.Instance.CalculateDamageOnUnit(target2, DamageType.Physical, Damage.WDamage()) ||
                             target2.Distance(Player.Instance) > W.Range) &&
                            Player.Instance.CalculateDamageOnUnit(target2, DamageType.Physical,
                                Damage.RDamage(target2) + Damage.WDamage()) > target2.Health)
                        {
                            R.Cast(target2);
                        }
                    }
                }

                target = (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) ||
                          Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                    ? TargetSelector.GetTarget(E.Range + W.Range, DamageType.Physical)
                    : (Obj_AI_Base)Orbwalker.LastTarget;
                if (Q.IsReady() && Orbwalker.ActiveModesFlags != Orbwalker.ActiveModes.None || ComboMenu["FlashBurst"].Cast<KeyBind>().CurrentValue && target != null)
                {
                    Player.CastSpell(SpellSlot.Q, target.Position);
                    return;
                }
                return;
            }

            //E
            if (args.SData.Name.ToLower().Contains(E.Name.ToLower()))
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    if (Player.Instance.HasBuff("RivenFengShuiEngine") && R.IsReady() &&
                        ComboMenu["R2Combo"].Cast<CheckBox>().CurrentValue)
                    {
                        ssfl = true;
                        var target2 = TargetSelector.GetTarget(R.Range, DamageType.Physical);
                        if (target2 != null &&
                            Player.Instance.CalculateDamageOnUnit(target2, DamageType.Physical,
                                (Damage.RDamage(target2))) > target2.Health)
                        {
                            R.Cast(target2);
                            return;
                        }
                    }
                    if ((EnableR == true) && R.IsReady() &&
                        !Player.Instance.HasBuff("RivenFengShuiEngine") &&
                        ComboMenu["RCombo"].Cast<CheckBox>().CurrentValue)
                    {
                        ssfl = false;
                        Player.CastSpell(SpellSlot.R);
                    }
                    target = TargetSelector.GetTarget(W.Range, DamageType.Physical);
                    if (target != null && Player.Instance.Distance(target) < W.Range)
                    {
                        Player.CastSpell(SpellSlot.W);
                        return;
                    }
                }
            }

            //Q
            if (args.SData.Name.ToLower().Contains(Q.Name.ToLower()))
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    if (Player.Instance.HasBuff("RivenFengShuiEngine") && R.IsReady() &&
                        ComboMenu["R2Combo"].Cast<CheckBox>().CurrentValue)
                    {
                        var target2 = TargetSelector.GetTarget(R.Range, DamageType.Physical);
                        if (target2 != null &&
                            (target2.Distance(Player.Instance) < 300 &&
                             target2.Health >
                             Player.Instance.CalculateDamageOnUnit(target2, DamageType.Physical, Damage.QDamage()) ||
                             target2.Distance(Player.Instance) > 300) &&
                            Player.Instance.CalculateDamageOnUnit(target2, DamageType.Physical,
                                Damage.RDamage(target2) + Damage.QDamage()) > target2.Health)
                        {
                            R.Cast(target2);
                        }
                    }
                }
                return;
            }

            if (args.SData.IsAutoAttack() && target != null)
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    ComboAfterAa(target);
                }

                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                {
                    HarassAfterAa(target);
                }

                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
                {
                    JungleAfterAa(target);
                }

                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit) && target.IsMinion())
                {
                    LastHitAfterAa(target);
                }

                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) && target.IsMinion())
                {
                    LaneClearAfterAa(target);
                }
            }
        }

        private static float getComboDamage(Obj_AI_Base enemy)
        {
            if (enemy != null)
            {
                float damage = 0;
                float passivenhan;
                if (_Player.Level >= 18)
                {
                    passivenhan = 0.5f;
                }
                else if (_Player.Level >= 15)
                {
                    passivenhan = 0.45f;
                }
                else if (_Player.Level >= 12)
                {
                    passivenhan = 0.4f;
                }
                else if (_Player.Level >= 9)
                {
                    passivenhan = 0.35f;
                }
                else if (_Player.Level >= 6)
                {
                    passivenhan = 0.3f;
                }
                else if (_Player.Level >= 3)
                {
                    passivenhan = 0.25f;
                }
                else
                {
                    passivenhan = 0.2f;
                }
                if (Item.HasItem(3074)) damage = damage + _Player.GetAutoAttackDamage(enemy) * 0.7f;
                if (W.IsReady()) damage = damage + ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.W);
                if (Q.IsReady())
                {
                    var qnhan = 4 - QCount;
                    damage = damage + ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.Q) * qnhan +
                             _Player.GetAutoAttackDamage(enemy) * qnhan * (1 + passivenhan);
                }
                damage = damage + _Player.GetAutoAttackDamage(enemy) * (1 + passivenhan);
                if (R.IsReady())
                {
                    return damage * 1.2f + ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.R);
                }

                return damage;
            }
            return 0;
        }

        public static void ComboAfterAa(Obj_AI_Base target)
        {
            try
            {
                if (Player.Instance.HasBuff("RivenFengShuiEngine") && R.IsReady() &&
                    ComboMenu["R2콤보"].Cast<CheckBox>().CurrentValue)
                {
                    if (Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical, Damage.RDamage(target)) + Player.Instance.GetAutoAttackDamage(target, true) > target.Health)
                    {
                        ssfl = true;
                        R.Cast(target);
                        return;
                    }
                }
                if (ComboMenu["W콤보"].Cast<CheckBox>().CurrentValue && W.IsReady() &&
                    W.IsInRange(target))
                {
                    Core.DelayAction(ForceItem, 50);
                    Player.CastSpell(SpellSlot.W);
                    return;
                }
                if (ComboMenu["Q콤보"].Cast<CheckBox>().CurrentValue && Q.IsReady())
                {
                    Player.CastSpell(SpellSlot.Q, target.Position);
                    return;
                }
                Core.DelayAction(ForceItem, 50);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        
        }

        public static void HarassAfterAa(Obj_AI_Base target)
        {
            
                if (HarassMenu["WHarass"].Cast<CheckBox>().CurrentValue && W.IsReady() &&
                    W.IsInRange(target))
                {
                Core.DelayAction(ForceItem, 50);
                Player.CastSpell(SpellSlot.W);
                return;
            }
                if (HarassMenu["QHarass"].Cast<CheckBox>().CurrentValue && Q.IsReady())
                {
                    Player.CastSpell(SpellSlot.Q, target.Position);
                return;
            }
            Core.DelayAction(ForceItem, 50);


        }

        public static void LastHitAfterAa(Obj_AI_Base target)
        {
            
                var unitHp = target.Health - Player.Instance.GetAutoAttackDamage(target, true);
                if (unitHp > 0)
                {
                    if (FarmingMenu["QLastHit"].Cast<CheckBox>().CurrentValue && Q.IsReady() &&
                        Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical, Damage.QDamage()) >
                        unitHp)
                    {
                        Player.CastSpell(SpellSlot.Q, target.Position);
                        return;
                    }
                    if (FarmingMenu["WLastHit"].Cast<CheckBox>().CurrentValue && W.IsReady() &&
                        W.IsInRange(target) &&
                        Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical, Damage.WDamage()) >
                        unitHp)
                    {
                        Player.CastSpell(SpellSlot.W);
                    }
                }
            
        }

        public static void LaneClearAfterAa(Obj_AI_Base target)
        {
            try
            { 
                var unitHp = target.Health - Player.Instance.GetAutoAttackDamage(target, true);
                if (unitHp > 0)
                {
                    if (FarmingMenu["Q라인클리어"].Cast<CheckBox>().CurrentValue && Q.IsReady())
                    {
                        Player.CastSpell(SpellSlot.Q, target.Position);
                        return;
                    }
                    if (FarmingMenu["W라인클리어"].Cast<CheckBox>().CurrentValue && W.IsReady() &&
                        W.IsInRange(target))
                    {
                        Player.CastSpell(SpellSlot.W);
                        return;
                    }
                }
                else
                {
                    List<Obj_AI_Minion> minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                        Player.Instance.Position, Q.Range).Where(a => a.NetworkId != target.NetworkId).ToList();
                    if (FarmingMenu["Q라인클리어"].Cast<CheckBox>().CurrentValue && Q.IsReady() && minions.Any())
                    {
                        Player.CastSpell(SpellSlot.Q, minions[0].Position);
                        return;
                    }
                    minions = minions.Where(a => a.Distance(Player.Instance) < W.Range).ToList();
                    if (FarmingMenu["W라인클리어"].Cast<CheckBox>().CurrentValue && W.IsReady() &&
                        W.IsInRange(target) && minions.Any())
                    {
                        Player.CastSpell(SpellSlot.W);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex);
            }
        }

        public static void JungleAfterAa(Obj_AI_Base target)
        {
            
            {
                if (FarmingMenu["W정글클리어"].Cast<CheckBox>().CurrentValue && W.IsReady() &&
                    W.IsInRange(target))
                {
                    Core.DelayAction(ForceItem, 50);
                    Player.CastSpell(SpellSlot.W);
                    return;
                }
                if (FarmingMenu["Q정글클리어"].Cast<CheckBox>().CurrentValue && Q.IsReady())
                {
                    Player.CastSpell(SpellSlot.Q, target.Position);
                    return;
                }
                Core.DelayAction(ForceItem, 50);
            }
        }
        public static int SkinId()
        {
            return Skin["스킨.Id"].Cast<Slider>().CurrentValue;
        }
        public static bool checkSkin()
        {
            return Skin["스킨체크"].Cast<CheckBox>().CurrentValue;
        }
        private static void Combo()
        {
            
            if (Orbwalker.IsAutoAttacking) return;
            var target = TargetSelector.GetTarget(E.Range + W.Range + 200, DamageType.Physical);
            if (target == null) return;
            var useQ = ComboMenu["Q콤보"].Cast<CheckBox>().CurrentValue;
            var useW = ComboMenu["W콤보"].Cast<CheckBox>().CurrentValue;
            var useE = ComboMenu["E콤보"].Cast<CheckBox>().CurrentValue;
            var useR = ComboMenu["R콤보"].Cast<CheckBox>().CurrentValue;
            var useItem = ComboMenu["티아멧사용"].Cast<CheckBox>().CurrentValue;
            EnableR = false;
            try
            { 
                if (R.IsReady() && Player.Instance.HasBuff("RivenFengShuiEngine") &&
                     ComboMenu["R2콤보"].Cast<CheckBox>().CurrentValue)
            {
                if (EntityManager.Heroes.Enemies.Where(
                        enemy =>
                            enemy.IsValidTarget(R.Range) &&
                            enemy.Health <
                            Player.Instance.CalculateDamageOnUnit(enemy, DamageType.Physical,
                                Damage.RDamage(enemy))).Any(enemy => R.Cast(enemy)))
                {
                    ssfl = true;
                    return;
                }
            }

            if (ComboMenu["R콤보"].Cast<CheckBox>().CurrentValue && R.IsReady() && !Player.Instance.HasBuff("RivenFengShuiEngine"))
            {
                if ((ComboMenu["RCantKill"].Cast<CheckBox>().CurrentValue &&
                    target.Health > Damage.ComboDamage(target, true)
                    && target.Health < Damage.ComboDamage(target)
                    && target.Health > Player.Instance.GetAutoAttackDamage(target, true) * 2) ||
                    (ComboMenu["REnemyCount"].Cast<Slider>().CurrentValue > 0 &&
                    Player.Instance.CountEnemiesInRange(600) >= ComboMenu["REnemyCount"].Cast<Slider>().CurrentValue) || IsRActive)
                {
                    ssfl = false;
                    EnableR = true;
                }
                if (ComboMenu["강제R"].Cast<KeyBind>().CurrentValue)
                {
                    ssfl = false;
                    EnableR = true;
                }
            }

            if (ComboMenu["E콤보"].Cast<CheckBox>().CurrentValue && target.Distance(Player.Instance) > W.Range && E.IsReady())
            {
                if (Item.HasItem(3142) && Item.CanUseItem(3142))
                {
                    Item.UseItem(3142);
                }
                Player.CastSpell(SpellSlot.E, target.Position);
                return;
            }

                if (ComboMenu["E콤보"].Cast<CheckBox>().CurrentValue && target.Distance(Player.Instance) < W.Range && E.IsReady())
                {
                    Player.CastSpell(SpellSlot.E, Game.CursorPos);
                    return;
                }

                if (ComboMenu["W콤보"].Cast<CheckBox>().CurrentValue &&
                target.Distance(Player.Instance) <= W.Range && W.IsReady())
            {
                    Core.DelayAction(ForceItem, 50);
                    Player.CastSpell(SpellSlot.W);
                    return;
                
            }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        private static void Flee()
        {         
            var x = _Player.Position.Extend(Game.CursorPos, 300);
            if (Q.IsReady() && !_Player.IsDashing()) Player.CastSpell(SpellSlot.Q, Game.CursorPos);
            if (E.IsReady() && !_Player.IsDashing()) Player.CastSpell(SpellSlot.E, x.To3D());

        }

        public static void Harass()
        {
            if (Orbwalker.IsAutoAttacking) return;

            var target = TargetSelector.GetTarget(E.Range + W.Range, DamageType.Physical);

             {
                if (target == null) return;

                if (HarassMenu["EHarass"].Cast<CheckBox>().CurrentValue &&
                    (target.Distance(Player.Instance) > W.Range &&
                     target.Distance(Player.Instance) < E.Range + W.Range ||
                     IsRActive && R.IsReady() &&
                     target.Distance(Player.Instance) < E.Range + 265 + Player.Instance.BoundingRadius) &&
                    E.IsReady())
                {
                    Player.CastSpell(SpellSlot.E, target.Position);
                    return;
                }

                if (HarassMenu["WHarass"].Cast<CheckBox>().CurrentValue &&
                    target.Distance(Player.Instance) <= W.Range && W.IsReady())
                {
                    ForceItem();
                    Player.CastSpell(SpellSlot.W);
                    return;
                }
            }
        }

        private static void JungleClear()
        {
            var minion =
                 EntityManager.MinionsAndMonsters.Monsters.OrderByDescending(a => a.MaxHealth)
                     .FirstOrDefault(a => a.Distance(Player.Instance) < Player.Instance.GetAutoAttackRange(a));

            {
                if (minion == null) return;

                if (FarmingMenu["QJungleClear"].Cast<CheckBox>().CurrentValue && Q.IsReady() &&
                       minion.Health <=
                       Player.Instance.CalculateDamageOnUnit(minion, DamageType.Physical, Damage.QDamage()))
                {
                    Player.CastSpell(SpellSlot.Q, minion.Position);
                    return;
                }

                if (FarmingMenu["EJungleClear"].Cast<CheckBox>().CurrentValue && (!W.IsReady() && !Q.IsReady() || Player.Instance.HealthPercent < 20) && E.IsReady() &&
                    LastCastW + 750 < Environment.TickCount)
                {
                    Player.CastSpell(SpellSlot.E, minion.Position);
                }
            }
        }
        public static void LaneClear()
        {
            try
            {
                Orbwalker.ForcedTarget = null;
                {
                    if (Orbwalker.IsAutoAttacking || LastCastQ + 260 > Environment.TickCount) return;
                    foreach (
                        var minion in EntityManager.MinionsAndMonsters.EnemyMinions.Where(a => a.IsValidTarget(W.Range)))
                    {
                        if (FarmingMenu["QLaneClear"].Cast<CheckBox>().CurrentValue && Q.IsReady() &&
                            minion.Health <=
                            Player.Instance.CalculateDamageOnUnit(minion, DamageType.Physical, Damage.QDamage()))
                        {
                            Player.CastSpell(SpellSlot.Q, minion.Position);
                            return;
                        }
                        if (FarmingMenu["WLaneClear"].Cast<CheckBox>().CurrentValue && W.IsReady() &&
                            minion.Health <=
                            Player.Instance.CalculateDamageOnUnit(minion, DamageType.Physical, Damage.WDamage()))
                        {
                            Player.CastSpell(SpellSlot.W);
                            return;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Console.Write(ex);
            }

        }

           
        public static void LastHit()
        {
            Orbwalker.ForcedTarget = null;
            {
                if (Orbwalker.IsAutoAttacking) return;

                foreach (
                    var minion in EntityManager.MinionsAndMonsters.EnemyMinions.Where(a => a.IsValidTarget(W.Range)))
                {
                    if (FarmingMenu["QLastHit"].Cast<CheckBox>().CurrentValue && Q.IsReady() &&
                        minion.Health <=
                        Player.Instance.CalculateDamageOnUnit(minion, DamageType.Physical, Damage.QDamage()))
                    {
                        Player.CastSpell(SpellSlot.Q, minion.Position);
                        return;
                    }
                    if (FarmingMenu["WLastHit"].Cast<CheckBox>().CurrentValue && W.IsReady() &&
                        minion.Health <=
                        Player.Instance.CalculateDamageOnUnit(minion, DamageType.Physical, Damage.WDamage()))
                    {
                        Player.CastSpell(SpellSlot.W);
                        return;
                    }
                }
            }

        }
        public static bool IsRActive
        {
            get
            {
                return ComboMenu["ForcedR"].Cast<KeyBind>().CurrentValue &&
                       ComboMenu["RCombo"].Cast<CheckBox>().CurrentValue;
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (DrawMenu["drawCombo"].Cast<CheckBox>().CurrentValue)
            {
                Drawing.DrawCircle(_Player.Position, Q.Range + E.Range, Color.Red);
            }
            if (DrawMenu["drawFBurst"].Cast<CheckBox>().CurrentValue)
            {
                Drawing.DrawCircle(_Player.Position, 425 + E.Range, Color.Green);
            }
            if (DrawMenu["drawStatus"].Cast<CheckBox>().CurrentValue)
            {                
                var pos = Drawing.WorldToScreen(Player.Instance.Position);
                Drawing.DrawText((int)pos.X - 45, (int)pos.Y + 40, Color.DarkGray, "Forced R: " + IsRActive);
            }
        }
        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (_Player.IsDead)
                return;
            if (!DrawMenu["DrawDamage"].Cast<CheckBox>().CurrentValue) return;
            foreach (var aiHeroClient in EntityManager.Heroes.Enemies)
            {
                if(aiHeroClient.Distance(_Player) < 1000)
                {               
                var pos = new Vector2(aiHeroClient.HPBarPosition.X + _xOffset, aiHeroClient.HPBarPosition.Y + _yOffset);
                var fullbar = (_barLength) * (aiHeroClient.HealthPercent / 100);
                var damage = (_barLength) *
                                 ((getComboDamage(aiHeroClient) / aiHeroClient.MaxHealth) > 1
                                     ? 1
                                     : (getComboDamage(aiHeroClient) / aiHeroClient.MaxHealth));
                Line.DrawLine(Color.Gray, 9f, new Vector2(pos.X, pos.Y),
                    new Vector2(pos.X + (damage > fullbar ? fullbar : damage), pos.Y));
                Line.DrawLine(Color.Black, 9, new Vector2(pos.X + (damage > fullbar ? fullbar : damage) - 2, pos.Y), new Vector2(pos.X + (damage > fullbar ? fullbar : damage) + 2, pos.Y));
                }
                else
                {
                    return;
                }
            }
        }

    }
}