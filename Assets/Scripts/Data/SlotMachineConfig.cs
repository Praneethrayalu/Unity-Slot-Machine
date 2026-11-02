using UnityEngine;

namespace SlotGame.Data
{
    /// <summary>
    /// Session economy and pacing settings, kept out of code so designers can tune them.
    /// </summary>
    [CreateAssetMenu(fileName = "SlotMachineConfig", menuName = "Slot Game/Slot Machine Config", order = 4)]
    public sealed class SlotMachineConfig : ScriptableObject
    {
        [Header("Economy")]
        [Min(0)] [SerializeField] private long startingCredits = 1000;

        [Tooltip("Credits offered when the player runs out.")]
        [Min(1)] [SerializeField] private long refillCredits = 1000;

        [Tooltip("Allowed bets, strictly ascending.")]
        [SerializeField] private int[] betOptions = { 10, 20, 50, 100 };

        [Min(0)] [SerializeField] private int defaultBetIndex;

        [Header("Spin pacing (seconds)")]
        [Tooltip("Delay between consecutive reels starting.")]
        [Min(0)] [SerializeField] private float reelStartStagger = 0.08f;

        [Tooltip("Minimum time all reels spin before the first one stops.")]
        [Min(0)] [SerializeField] private float minimumSpinTime = 0.9f;

        [Tooltip("Delay between consecutive reels stopping.")]
        [Min(0)] [SerializeField] private float reelStopStagger = 0.32f;

        [Tooltip("Extra spin time on the last reel when the stopped reels can still form a win.")]
        [Min(0)] [SerializeField] private float anticipationTime = 1.4f;

        [Tooltip("Stagger used when the player slams the spin button to stop early.")]
        [Min(0)] [SerializeField] private float quickStopStagger = 0.07f;

        [Header("Celebration")]
        [Tooltip("Wins at or above this multiplier show the BIG WIN popup.")]
        [Min(1)] [SerializeField] private int bigWinMultiplier = 5;

        [Tooltip("Wins at or above this multiplier show the MEGA WIN popup.")]
        [Min(1)] [SerializeField] private int megaWinMultiplier = 10;

        [Tooltip("Wins at or above this multiplier (or a line of wilds) are a JACKPOT.")]
        [Min(1)] [SerializeField] private int jackpotMultiplier = 100;

        [Min(0)] [SerializeField] private float smallWinPresentationTime = 1.2f;
        [Min(0)] [SerializeField] private float bigWinPresentationTime = 3.2f;
        [Min(0)] [SerializeField] private float jackpotPresentationTime = 5f;

        [Header("Auto & free spins")]
        [Min(1)] [SerializeField] private int autoSpinCount = 25;

        [Tooltip("Pause between automatic spins (auto-spin and free spins).")]
        [Min(0)] [SerializeField] private float autoSpinDelay = 0.45f;

        public long StartingCredits => startingCredits;
        public long RefillCredits => refillCredits;
        public int[] BetOptions => betOptions;
        public int DefaultBetIndex => defaultBetIndex;
        public float ReelStartStagger => reelStartStagger;
        public float MinimumSpinTime => minimumSpinTime;
        public float ReelStopStagger => reelStopStagger;
        public float AnticipationTime => anticipationTime;
        public float QuickStopStagger => quickStopStagger;
        public int BigWinMultiplier => bigWinMultiplier;
        public int MegaWinMultiplier => megaWinMultiplier;
        public int JackpotMultiplier => jackpotMultiplier;
        public float SmallWinPresentationTime => smallWinPresentationTime;
        public float BigWinPresentationTime => bigWinPresentationTime;
        public float JackpotPresentationTime => jackpotPresentationTime;
        public int AutoSpinCount => autoSpinCount;
        public float AutoSpinDelay => autoSpinDelay;
    }
}
