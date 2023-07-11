using AntiCheat;
using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NeonMiracle
{
    public class NeonMiracle : MelonMod
    {
        public static Game Game { get; private set; }

        private MelonPreferences_Category _bookOfPowerCategory;
        private readonly MelonPreferences_Entry<string>[] _controlSettings = new MelonPreferences_Entry<string>[4];
        //private MelonPreferences_Entry<char> _telefragKey;

        private readonly InputAction[] actions = new InputAction[] { new InputAction(), new InputAction(), new InputAction(), new InputAction()/*, new InputAction(), new InputAction() */};

        //private readonly MethodInfo GetZiplinePoint = typeof(MechController).GetMethod("GetZiplinePoint", BindingFlags.NonPublic | BindingFlags.Instance);
        //private readonly MethodInfo GetTelefragTarget = typeof(MechController).GetMethod("GetTelefragTarget", BindingFlags.NonPublic | BindingFlags.Instance); Disabled for now

        private readonly float[] _cooldowns = new float[5]; // Purify, Elevate, Dashes, Stomp, Zipline
        private const float COOLDOWN_ELEVATE = .5f, COOLDOWN_GODSPEED = .5f, COOLDOWN_STOMP = .3f, COOLDOWN_PURIFY = 1f/*, COOLDOWN_ZIPLINE = 999*/;
        private const byte PURIFY_CHARGES = 2;
        private byte purifyCharges = PURIFY_CHARGES;

        private readonly Color colorHolder = new(0f, 0f, 0f, 0.6f);
        private readonly Vector2 pivot = new(0f, 0.5f);
        private readonly Vector3 scaleHolder = new(0.6f, 0.06f, 1f), scaleBar = new(0.5f, 1f, 1f);

        private readonly Transform[] cooldownBars = new Transform[4];


        //private GUIStyle _style;


        public override void OnApplicationLateStart()
        {
            Anticheat.TriggerAnticheat();
            Anticheat.Register("NeonMiracle");
            Game = Singleton<Game>.Instance;
            PatchGame();
            CreateSettings();
            Game.OnLevelLoadComplete += OnLevelLoadComplete;

            //_style = new()
            //{
            //    fontSize = 18,
            //    fontStyle = FontStyle.Bold
            //};
            //_style.normal.textColor = Color.gray;
        }

        private void OnLevelLoadComplete()
        {
            if (Game.GetCurrentLevel().levelID == "HUB_HEAVEN") return;

            GameObject bars = new("Bars");
            bars.transform.parent = GameObject.Find("HUD/Crosshair/").transform;
            bars.AddComponent<Canvas>();
            bars.transform.localPosition = new(2.4f, -0.1f, 0f);
            bars.transform.localScale = new(0.01f, 0.02f, 1f);
            bars.layer = 5;

            GameObject elevateHolder = new("ElevateHolder", typeof(RectTransform));
            elevateHolder.transform.SetParent(bars.transform, false);
            elevateHolder.transform.localScale = scaleHolder;
            elevateHolder.transform.localPosition = new(-45f, 25f, 0f);
            elevateHolder.GetComponent<RectTransform>().pivot = pivot;
            elevateHolder.AddComponent<Image>().color = colorHolder;

            GameObject barElevate = new("Elevate", typeof(RectTransform));
            barElevate.transform.SetParent(elevateHolder.transform, false);
            barElevate.transform.localScale = scaleBar;
            barElevate.transform.localPosition = Vector3.zero;
            barElevate.GetComponent<RectTransform>().pivot = pivot;
            barElevate.AddComponent<Image>().color = new Color(1f, 1f, 0f, 1f);
            cooldownBars[0] = barElevate.transform;


            GameObject godspeedHolder = new("GodspeedHolder", typeof(RectTransform));
            godspeedHolder.transform.SetParent(bars.transform, false);
            godspeedHolder.transform.localScale = scaleHolder;
            godspeedHolder.transform.localPosition = new(-38f, -3f, 0f);
            godspeedHolder.GetComponent<RectTransform>().pivot = pivot;
            godspeedHolder.AddComponent<Image>().color = colorHolder;

            GameObject barGodspeed = new("Godspeed", typeof(RectTransform));
            barGodspeed.transform.SetParent(godspeedHolder.transform, false);
            barGodspeed.transform.localScale = scaleBar;
            barGodspeed.transform.localPosition = Vector3.zero;
            barGodspeed.GetComponent<RectTransform>().pivot = pivot;
            barGodspeed.AddComponent<Image>().color = new Color(0f, 0f, 1f);
            cooldownBars[1] = barGodspeed.transform;


            GameObject stompHolder = new("StompHolder", typeof(RectTransform));
            stompHolder.transform.SetParent(bars.transform, false);
            stompHolder.transform.localScale = scaleHolder;
            stompHolder.transform.localPosition = new(-38f, 11f, 0f);
            stompHolder.GetComponent<RectTransform>().pivot = pivot;
            stompHolder.AddComponent<Image>().color = colorHolder;

            GameObject barStomp = new("Stomp", typeof(RectTransform));
            barStomp.transform.SetParent(stompHolder.transform, false);
            barStomp.transform.localScale = scaleBar;
            barStomp.transform.localPosition = Vector3.zero;
            barStomp.GetComponent<RectTransform>().pivot = pivot;
            barStomp.AddComponent<Image>().color = new Color(0f, 1f, 0f);
            cooldownBars[2] = barStomp.transform;


            GameObject purifyHolder = new("PurifyHolder", typeof(RectTransform));
            purifyHolder.transform.SetParent(bars.transform, false);
            purifyHolder.transform.localScale = scaleHolder;
            purifyHolder.transform.localPosition = new(-45f, -17f, 0f);
            purifyHolder.GetComponent<RectTransform>().pivot = pivot;
            purifyHolder.AddComponent<Image>().color = colorHolder;

            GameObject barPurify = new("Purify", typeof(RectTransform));
            barPurify.transform.SetParent(purifyHolder.transform, false);
            barPurify.transform.localScale = scaleBar;
            barPurify.transform.localPosition = Vector3.zero;
            barPurify.GetComponent<RectTransform>().pivot = pivot;
            barPurify.AddComponent<Image>().color = new Color(0.6f, 0f, 1f);
            cooldownBars[3] = barPurify.transform;


            _cooldowns[0] = .5f;
            _cooldowns[1] = .5f;
            _cooldowns[2] = .5f;
            _cooldowns[3] = .5f;
            //_cooldowns[4] = .5f;

            purifyCharges = PURIFY_CHARGES;

            for (int i = 0; i < actions.Length; i++)
            {
                if (actions[i] != null)
                {
                    actions[i].Disable();
                    actions[i].Dispose();
                }
                actions[i] = new InputAction();
                actions[i].AddBinding("<Keyboard>/" + _controlSettings[i].Value);
                actions[i].Enable();
            }
        }

        //Beta Text
        //public override void OnGUI()
        //{
        //    if (!RM.mechController.GetIsAlive()) return;

        //    GUI.Label(new Rect(Camera.main.pixelWidth / 2, Camera.main.pixelHeight / 2, 75, 100), "Neon Miracle Beta", _style);
        //}

        public override void OnUpdate()
        {
            if (!RM.mechController || RM.time.GetIsTimeScaleZero()) return;
            MechController mechController = RM.mechController;

            float cooldown = Math.Max(0, _cooldowns[0] - Time.deltaTime);
            _cooldowns[0] = cooldown;
            cooldownBars[0].localScale = new(1f - (_cooldowns[0] / COOLDOWN_ELEVATE), 1f, 1f);

            cooldown = Math.Max(0, _cooldowns[1] - Time.deltaTime);
            _cooldowns[1] = cooldown;
            cooldownBars[1].localScale = new(1f - (_cooldowns[1] / COOLDOWN_GODSPEED), 1f, 1f);

            cooldown = Math.Max(0, _cooldowns[2] - Time.deltaTime);
            _cooldowns[2] = cooldown;
            cooldownBars[2].localScale = new(1f - (_cooldowns[2] / COOLDOWN_STOMP), 1f, 1f);

            cooldown = Math.Max(0, _cooldowns[3] - Time.deltaTime);
            _cooldowns[3] = cooldown;
            cooldownBars[3].localScale = new(purifyCharges == 0 ? 0 : (1f - (_cooldowns[3] / COOLDOWN_PURIFY)), 1f, 1f);


            if (actions[0].WasPressedThisFrame() && _cooldowns[0] == 0f)
            {
                mechController.DoDiscardAbility(PlayerCardData.DiscardAbility.Jump);
                _cooldowns[0] = COOLDOWN_ELEVATE;
            }
            if (actions[1].WasPressedThisFrame() && _cooldowns[1] == 0f)
            {
                mechController.DoDiscardAbility(PlayerCardData.DiscardAbility.Dash);
                _cooldowns[1] = COOLDOWN_GODSPEED;
            }
            if (actions[2].WasPressedThisFrame() && !RM.drifter.GetIsStomping() && _cooldowns[2] == 0f)
            {
                mechController.DoDiscardAbility(PlayerCardData.DiscardAbility.Stomp);
                _cooldowns[2] = COOLDOWN_STOMP;
            }
            if (actions[3].WasPerformedThisFrame() && purifyCharges > 0 && _cooldowns[3] == 0f)
            {
                mechController.DoDiscardAbility(PlayerCardData.DiscardAbility.Mine);
                _cooldowns[3] = COOLDOWN_PURIFY;
                purifyCharges--;
            }/*
            if (actions[4].WasPressedThisFrame() && _cooldowns[2] == 0f)
            {
                mechController.DoDiscardAbility(PlayerCardData.DiscardAbility.Fireball);
                _cooldowns[2] = COOLDOWN_DASHES;
            }
            if (actions[5].WasPressedThisFrame() && ((MechController.ZiplinePoint)GetZiplinePoint.Invoke(mechController, new object[0])).hasValidPoint && _cooldowns[4] == 0f)
            {
                mechController.DoDiscardAbility(PlayerCardData.DiscardAbility.Zipline);
                _cooldowns[4] = COOLDOWN_ZIPLINE;
            }*/
            //else if (chr == _telefragKey.Value && GetTelefragTarget.Invoke(mechController, new object[0]) != null)
            //    mechController.DoDiscardAbility(PlayerCardData.DiscardAbility.Telefrag);
        }

        private void PatchGame()
        {
            HarmonyLib.Harmony harmony = new("de.MOPSKATER.BookOfPower");

            MethodInfo target = typeof(LevelData).GetMethod("GetIsDiscardLocked");
            HarmonyMethod patch = new(typeof(NeonMiracle).GetMethod("PreGetIsDiscardLocked"));
            harmony.Patch(target, patch);

            //target = typeof(Game).GetMethod("OnLevelWin");
            //patch = new(typeof(NeonMiracle).GetMethod("PreventNewGhost"));
            //harmony.Patch(target, patch);

            //target = typeof(LevelRush).GetMethod("IsCurrentLevelRushScoreBetter", BindingFlags.NonPublic | BindingFlags.Static);
            //patch = new(typeof(NeonMiracle).GetMethod("PreventNewBestLevelRush"));
            //harmony.Patch(target, patch);

            //target = typeof(LevelStats).GetMethod("UpdateTimeMicroseconds");
            //patch = new(typeof(NeonMiracle).GetMethod("PreventNewScore"));
            //harmony.Patch(target, patch);

            //target = typeof(string).GetMethod("Concat", new Type[] { typeof(string[]) });
            //patch = new(typeof(NeonMiracle).GetMethod("PreConcat"));
            //harmony.Patch(target, patch);
        }

        private void CreateSettings()
        {
            _bookOfPowerCategory = MelonPreferences.CreateCategory("Neon Miracle");
            _controlSettings[0] = _bookOfPowerCategory.CreateEntry("Elevate", "e", description: "Key for Elevate");
            _controlSettings[1] = _bookOfPowerCategory.CreateEntry("Godspeed", "shift", description: "Key for Godspeed");
            _controlSettings[2] = _bookOfPowerCategory.CreateEntry("Stomp", "ctrl", description: "Key for Stomp");
            _controlSettings[3] = _bookOfPowerCategory.CreateEntry("Purify", "b", description: "Key for Purify");
            //_controlSettings[4] = _bookOfPowerCategory.CreateEntry("Fireball", "r", description: "Key for Fireball");
            //_controlSettings[5] = _bookOfPowerCategory.CreateEntry("Zipline", "t", description: "Key for Zipline");
            //_telefragKey = _bookOfPowerCategory.CreateEntry("Telefrag", "q", description: "Key for Telefrag");

            //COOLDOWN_PURIFY = _bookOfPowerCategory.CreateEntry("Purify cooldown", 1f);
            //COOLDOWN_ELEVATE = _bookOfPowerCategory.CreateEntry("Elevate cooldown", .5f);
            //COOLDOWN_DASHES = _bookOfPowerCategory.CreateEntry("Dashes cooldown", 1f);
            //COOLDOWN_STOMP = _bookOfPowerCategory.CreateEntry("Stomp cooldown", .3f);
            //COOLDOWN_ZIPLINE = _bookOfPowerCategory.CreateEntry("Zipline cooldown", 1f);
        }

        public static bool PreGetIsDiscardLocked(ref PlayerCardData cardData, ref bool __result)
        {
            __result = cardData.discardAbility != PlayerCardData.DiscardAbility.Telefrag;
            return false;
        }

        //public static bool PreventNewScore(LevelStats __instance, ref long newTime)
        //{
        //    if (newTime < __instance._timeBestMicroseconds)
        //    {
        //        if (__instance._timeBestMicroseconds == 999999999999L)
        //            __instance._timeBestMicroseconds = 600000000;
        //        __instance._newBest = true;
        //    }
        //    else
        //        __instance._newBest = false;
        //    __instance._timeLastMicroseconds = newTime;
        //    return false;
        //}

        //public static bool PreventNewGhost(Game __instance)
        //{
        //    __instance.winAction = null;
        //    return true;
        //}

        //public static bool PreventNewBestLevelRush(ref bool __result)
        //{
        //    __result = false;
        //    return false;
        //}

        //public static void PreConcat(ref string[] values)
        //{
        //    if (values.Length == 5 && values[4] == "medallog.txt")
        //        values[4] = "Neon Miracle Medallog.txt";
        //}
    }
}