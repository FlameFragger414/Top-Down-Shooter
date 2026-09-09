using UnityEngine;

namespace Neon
{
    public enum MaskId { Rooster = 0, Owl = 1, Tiger = 2, Wolf = 3, Pig = 4, Rabbit = 5 }

    public enum GameState { Title, MaskSelect, Playing, Dead, Clear }

    public class GameManager : MonoBehaviour
    {
        public static GameManager I;

        public GameState State { get; private set; } = GameState.Title;
        public MaskId Mask = MaskId.Rooster;
        public int Floor { get; private set; } = 1;
        public string FloorName { get; private set; } = "";
        public int Score { get; private set; }
        public int RunScore { get; private set; }
        public int Combo { get; private set; }
        public float ComboTime { get; private set; }
        public int Kills { get; private set; }
        public int TotalEnemies { get; private set; }
        public float FloorTime { get; private set; }
        float floorStart;
        public float FloorAge => Time.time - floorStart;
        public string LastEvent { get; private set; } = "";
        public float LastEventTime { get; private set; }

        public bool Paused => State != GameState.Playing;
        public bool AcceptsGameplayInput => State == GameState.Playing;
        public bool ExitLive => State == GameState.Playing && Enemy.AliveCount == 0;

        const float ComboWindow = 4.2f;

        public static readonly string[] MaskNames =
            { "ROOSTER", "OWL", "TIGER", "WOLF", "PIG", "RABBIT" };
        public static readonly string[] MaskPerks =
        {
            "YOU MOVE FASTER",
            "YOU SEE WHAT THEY SEE",
            "LONGER REACH, HARDER SWING",
            "YOU START WITH A KNIFE",
            "TWO EXTRA HITS BEFORE YOU DROP",
            "YOU DASH TWICE AS OFTEN",
        };

        public static void Create(Transform parent)
        {
            var go = new GameObject("Game");
            go.transform.SetParent(parent, false);
            I = go.AddComponent<GameManager>();
        }

        void Start()
        {
            Sfx.I.PlayTrack(0, 112f);
        }

        void Update()
        {
            switch (State)
            {
                case GameState.Title:
                    if (InputHub.ConfirmDown || InputHub.AttackDown)
                    {
                        Sfx.I.Play("ui", 0.6f);
                        State = GameState.MaskSelect;
                    }
                    break;

                case GameState.MaskSelect:
                    if (InputHub.LeftDown) { Cycle(-1); }
                    if (InputHub.RightDown) { Cycle(1); }
                    if (InputHub.ConfirmDown || InputHub.AttackDown)
                    {
                        Sfx.I.Play("pickup", 0.7f);
                        Floor = 1;
                        RunScore = 0;
                        StartFloor();
                    }
                    if (InputHub.BackDown) State = GameState.Title;
                    break;

                case GameState.Playing:
                    FloorTime += Time.deltaTime;
                    if (ComboTime > 0f)
                    {
                        ComboTime -= Time.deltaTime;
                        if (ComboTime <= 0f) Combo = 0;
                    }
                    if (InputHub.RestartDown) StartFloor();
                    if (ExitLive) CheckExit();
                    break;

                case GameState.Dead:
                    if (InputHub.RestartDown || InputHub.ConfirmDown) StartFloor();
                    break;

                case GameState.Clear:
                    if (InputHub.ConfirmDown)
                    {
                        Floor++;
                        StartFloor();
                    }
                    break;
            }
        }

        void Cycle(int dir)
        {
            int n = MaskNames.Length;
            Mask = (MaskId)(((int)Mask + dir + n) % n);
            Sfx.I.Play("ui", 0.5f);
        }

        // ------------------------------------------------------------------- floors
        public void StartFloor()
        {
            Time.timeScale = 1f;
            State = GameState.Playing;
            Score = 0;
            Kills = 0;
            Combo = 0;
            ComboTime = 0f;
            FloorTime = 0f;
            floorStart = Time.time;
            LastEvent = "";

            FX.I.ClearAll();

            char[,] grid;
            if (Floor == 1)
            {
                grid = LevelGen.FromAscii(Levels.Intro);
                FloorName = "WELCOME PARTY";
            }
            else
            {
                grid = LevelGen.Generate(Floor, out string generatedName);
                FloorName = generatedName;
            }

            LevelGen.I.Build(grid, Floor);
            TotalEnemies = Enemy.AliveCount;

            var start = LevelGen.I.PlayerStart;
            if (PlayerCtrl.I != null) Destroy(PlayerCtrl.I.gameObject);
            var p = PlayerCtrl.Spawn(start, "fist");
            CamCtrl.I.target = p.transform;
            CamCtrl.I.SnapTo(start);

            Sfx.I.PlayTrack(1 + (Floor % 4), 118f + (Floor % 5) * 6f);
        }

        void CheckExit()
        {
            if (PlayerCtrl.I == null || PlayerCtrl.I.Dead) return;
            if (Vector2.Distance(PlayerCtrl.I.transform.position, LevelGen.I.ExitPos) < 0.85f)
            {
                State = GameState.Clear;
                RunScore += Score + TimeBonus;
                Sfx.I.Play("clear", 0.8f);
                Sfx.I.Play("fanfare", 0.75f);
                Sfx.I.StopMusic(0.6f);
                Time.timeScale = 1f;
            }
        }

        public int TimeBonus => Mathf.Max(0, 1500 - Mathf.RoundToInt(FloorTime * 25f));

        public string Rank
        {
            get
            {
                int total = Score + TimeBonus;
                if (total > 6000) return "A+";
                if (total > 4500) return "A";
                if (total > 3200) return "B";
                if (total > 2200) return "C";
                if (total > 1200) return "D";
                return "E";
            }
        }

        // -------------------------------------------------------------------- events
        public void OnEnemyKilled(int baseScore, string cause)
        {
            Kills++;
            Combo++;
            ComboTime = ComboWindow;
            int gained = Mathf.RoundToInt(baseScore * (1f + (Combo - 1) * 0.35f));
            Score += gained;
            if (Combo >= 2)
                Sfx.I.Play("combo", 0.45f, 1f + Mathf.Min(Combo - 2, 8) * 0.09f);
            if (Combo >= 3) Flash(Combo + "X COMBO");
            if (Enemy.AliveCount == 0)
            {
                Flash("FLOOR CLEAR - FIND THE EXIT");
                Sfx.I.SetMusicPitch(1.06f);
            }
        }

        public void AddScore(int amount, string label)
        {
            Score += amount;
            Flash(label);
        }

        public void Flash(string msg)
        {
            LastEvent = msg;
            LastEventTime = Time.unscaledTime;
        }

        public void OnPlayerDied()
        {
            State = GameState.Dead;
            Time.timeScale = 1f;
            Sfx.I.SetMusicPitch(0.82f);
            Sfx.I.Play("sting", 0.75f);
        }
    }
}
