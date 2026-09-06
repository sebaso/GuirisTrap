// Lógica de dificultad pura (sin UnityEngine): la usan DifficultyManager,
// los tests de EditMode y el simulador de balance. Los valores por defecto
// DEBEN coincidir con los campos serializados de DifficultyManager — un test
// de paridad los compara para que no se separen.
public class DifficultyParams
{
    public float dayWeight = 0.6f;
    public float starsWeight = 0.4f;
    public float daySaturatesAt = 60f;

    public float spawnIntervalStart = 14f;
    public float spawnIntervalEnd = 10f;

    public int maxClientsStart = 6;
    public int maxClientsEnd = 12;

    public float[] groupWeightsStart = { 20f, 45f, 25f, 10f };
    public float[] groupWeightsEnd = { 5f, 30f, 35f, 30f };

    public int[] groupSizeUnlockDays = { 1, 2, 4, 6 };

    public float eventChanceStart = 0.5f;
    public float eventChanceEnd = 2f;

    public float specialChanceStart = 0.5f;
    public float specialChanceEnd = 2.5f;
    public int extraSpecialsPerDayEnd = 2;
    public int minPlayingDayForSpecials = 3;

    public static DifficultyParams GameDefaults() => new DifficultyParams();
}

public struct DifficultySnapshot
{
    public float difficulty01;
    public float spawnInterval;
    public int maxClients;
    public float eventMultiplier;
    public float specialMultiplier;
    public int extraSpecialsPerDay;
    public float[] groupWeights; // ya con desbloqueos aplicados (copia)
    public int maxUnlockedGroupSize;

    public float GroupWeight(int sizeIndex1to4) => groupWeights[sizeIndex1to4 - 1];
}

public static class DifficultyCurve
{
    public static DifficultySnapshot Evaluate(DifficultyParams p, int dayCompleted, float stars, int playingDayOverride = -1)
    {
        float dayT = p.daySaturatesAt <= 0f ? 1f : Clamp01(dayCompleted / p.daySaturatesAt);
        float starsT = Clamp01(stars / 5f);
        float d = Clamp01(dayT * p.dayWeight + starsT * p.starsWeight);

        int playingDay = playingDayOverride >= 0 ? playingDayOverride : dayCompleted + 1;

        var snap = new DifficultySnapshot
        {
            difficulty01 = d,
            spawnInterval = Lerp(p.spawnIntervalStart, p.spawnIntervalEnd, d),
            maxClients = RoundToInt(Lerp(p.maxClientsStart, p.maxClientsEnd, d)),
            eventMultiplier = Lerp(p.eventChanceStart, p.eventChanceEnd, d),
            specialMultiplier = Lerp(p.specialChanceStart, p.specialChanceEnd, d),
            extraSpecialsPerDay = RoundToInt(p.extraSpecialsPerDayEnd * d),
            groupWeights = ApplyGroupUnlocks(LerpGroupWeights(p, d), playingDay, p.groupSizeUnlockDays),
        };
        snap.maxUnlockedGroupSize = MaxUnlockedGroupSize(playingDay, p.groupSizeUnlockDays);
        return snap;
    }

    public static float[] LerpGroupWeights(DifficultyParams p, float difficulty01)
    {
        var w = new float[4];
        for (int i = 0; i < 4; i++)
            w[i] = Lerp(p.groupWeightsStart[i], p.groupWeightsEnd[i], difficulty01);
        return w;
    }

    //pone a 0 el peso de los tamaños aún no desbloqueados según el día jugado
    public static float[] ApplyGroupUnlocks(float[] weights, int playingDay, int[] unlockDays)
    {
        int firstAvailable = -1;

        for (int i = 0; i < weights.Length; i++)
        {
            int unlockDay = Max(1, unlockDays[i]);
            if (playingDay < unlockDay)
                weights[i] = 0f;
            else if (firstAvailable < 0)
                firstAvailable = i;
        }

        //el calendario no puede dejar al spawner sin tamaños posibles
        if (firstAvailable < 0)
            weights[0] = 1f;
        else if (weights[firstAvailable] <= 0f)
            weights[firstAvailable] = 1f;

        return weights;
    }

    public static int MaxUnlockedGroupSize(int playingDay, int[] unlockDays)
    {
        int max = 1;
        for (int i = 0; i < unlockDays.Length; i++)
            if (playingDay >= Max(1, unlockDays[i]))
                max = i + 1;
        return max;
    }

    public static bool SpecialsUnlocked(int minPlayingDayForSpecials, int dayCompleted)
        => dayCompleted + 1 >= minPlayingDayForSpecials;

    // ── equivalentes Mathf sin UnityEngine (half-to-even, como Mathf.RoundToInt) ──

    public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);

    public static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

    public static int Max(int a, int b) => a > b ? a : b;

    public static int RoundToInt(float f) => (int)System.Math.Round(f, System.MidpointRounding.ToEven);
}
