using System.Diagnostics;
using System.Text;
using SlotGame.Core.Game;
using SlotGame.Core.Random;
using SlotGame.Core.Reels;
using SlotGame.Core.Rules;
using SlotGame.Data;
using UnityEditor;
using UnityEngine;

namespace SlotGame.Editor
{
    /// <summary>
    /// Designer tool: shows the exact RTP of the current reel strips + pay table and validates it
    /// by playing the real engine for N spins. Menu: <b>Slot Game ▸ RTP Simulator</b>.
    /// </summary>
    public sealed class RtpSimulatorWindow : EditorWindow
    {
        [SerializeField] private ReelStripSet reelStrips;
        [SerializeField] private PayTable payTable;
        [SerializeField] private SymbolDatabase symbols;
        [SerializeField] private int spins = 1_000_000;
        [SerializeField] private int seed = 2024;

        private string _report = string.Empty;
        private Vector2 _scroll;

        [MenuItem("Slot Game/RTP Simulator", priority = 40)]
        public static void Open()
        {
            var window = GetWindow<RtpSimulatorWindow>("RTP Simulator");
            window.minSize = new Vector2(420, 360);
            if (window.reelStrips == null)
            {
                window.reelStrips = AssetDatabase.LoadAssetAtPath<ReelStripSet>("Assets/Data/ReelStripSet.asset");
            }

            if (window.payTable == null)
            {
                window.payTable = AssetDatabase.LoadAssetAtPath<PayTable>("Assets/Data/PayTable.asset");
            }

            if (window.symbols == null)
            {
                window.symbols = AssetDatabase.LoadAssetAtPath<SymbolDatabase>("Assets/Data/SymbolDatabase.asset");
            }
        }

        private void OnGUI()
        {
            reelStrips = (ReelStripSet)EditorGUILayout.ObjectField("Reel Strips", reelStrips, typeof(ReelStripSet), false);
            payTable = (PayTable)EditorGUILayout.ObjectField("Pay Table", payTable, typeof(PayTable), false);
            symbols = (SymbolDatabase)EditorGUILayout.ObjectField("Symbols", symbols, typeof(SymbolDatabase), false);
            spins = Mathf.Max(1, EditorGUILayout.IntField("Paid spins", spins));
            seed = EditorGUILayout.IntField("Seed", seed);

            using (new EditorGUI.DisabledScope(reelStrips == null || payTable == null))
            {
                if (GUILayout.Button("Calculate exact RTP + simulate", GUILayout.Height(28)))
                {
                    _report = Run();
                }
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.TextArea(_report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private string Run()
        {
            ReelSet reels = reelStrips.CreateReelSet();
            TheoreticalReturn exact = PayTableMath.Calculate(reels, payTable);

            var timer = Stopwatch.StartNew();
            SimulationReport sim = SlotSimulator.Run(reels, payTable, new SeededRandomProvider(seed), spins, 1);
            timer.Stop();

            var text = new StringBuilder();
            text.AppendLine($"EXACT ({exact.Combinations:N0} combinations)");
            text.AppendLine($"  Base game return : {exact.BaseGameReturn:P3}");
            text.AppendLine($"  Free spins / spin: {exact.ExpectedFreeSpinsPerSpin:F4}");
            text.AppendLine($"  Total RTP        : {exact.TotalReturn:P3}");
            text.AppendLine($"  Hit frequency    : {exact.HitFrequency:P2}");
            text.AppendLine();
            text.AppendLine($"SIMULATED ({timer.ElapsedMilliseconds:N0} ms, seed {seed})");
            text.AppendLine($"  {sim}");
            foreach (var pair in sim.WinsBySymbol)
            {
                string name = symbols != null && symbols.TryGet(pair.Key, out SymbolDefinition s) ? s.DisplayName : pair.Key.ToString();
                text.AppendLine($"  {name,-8} x3 : {pair.Value:N0} times (1 in {(double)(sim.PaidSpins + sim.FreeSpins) / pair.Value:N0})");
            }

            return text.ToString();
        }
    }
}
