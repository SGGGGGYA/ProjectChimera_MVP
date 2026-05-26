using System;

[Serializable]
public enum AIDifficulty
{
    Easy,
    Normal,
    Hard,
    Boss
}

[Serializable]
public class AIDifficultyProfile
{
    public AIDifficulty difficulty = AIDifficulty.Normal;

    public float skillUseChance = 0.6f;
    public float tacticalDepth = 0.5f;
    public float deathDoorFocus = 0.7f;
    public float emergencyHealThreshold = 0.4f;
    public int stressTargetThreshold = 50;
    public int maxSameSkillPerFight = 3;
    public float aoePreference = 0.5f;
    public float targetVariance = 0.2f;
    public bool useCooldowns = true;

    public static AIDifficultyProfile GetDefault(AIDifficulty diff)
    {
        switch (diff)
        {
            case AIDifficulty.Easy:
                return new AIDifficultyProfile
                {
                    difficulty = AIDifficulty.Easy,
                    skillUseChance = 0.35f,
                    tacticalDepth = 0.3f,
                    deathDoorFocus = 0.3f,
                    emergencyHealThreshold = 0.25f,
                    stressTargetThreshold = 70,
                    maxSameSkillPerFight = 2,
                    aoePreference = 0.2f,
                    targetVariance = 0.4f,
                    useCooldowns = false
                };
            case AIDifficulty.Normal:
                return new AIDifficultyProfile
                {
                    difficulty = AIDifficulty.Normal,
                    skillUseChance = 0.6f,
                    tacticalDepth = 0.5f,
                    deathDoorFocus = 0.7f,
                    emergencyHealThreshold = 0.4f,
                    stressTargetThreshold = 50,
                    maxSameSkillPerFight = 3,
                    aoePreference = 0.5f,
                    targetVariance = 0.2f,
                    useCooldowns = true
                };
            case AIDifficulty.Hard:
                return new AIDifficultyProfile
                {
                    difficulty = AIDifficulty.Hard,
                    skillUseChance = 0.85f,
                    tacticalDepth = 0.8f,
                    deathDoorFocus = 0.95f,
                    emergencyHealThreshold = 0.55f,
                    stressTargetThreshold = 35,
                    maxSameSkillPerFight = 4,
                    aoePreference = 0.7f,
                    targetVariance = 0.1f,
                    useCooldowns = true
                };
            case AIDifficulty.Boss:
                return new AIDifficultyProfile
                {
                    difficulty = AIDifficulty.Boss,
                    skillUseChance = 1.0f,
                    tacticalDepth = 1.0f,
                    deathDoorFocus = 1.0f,
                    emergencyHealThreshold = 0.65f,
                    stressTargetThreshold = 25,
                    maxSameSkillPerFight = 5,
                    aoePreference = 0.9f,
                    targetVariance = 0.05f,
                    useCooldowns = true
                };
            default:
                return new AIDifficultyProfile();
        }
    }
}
