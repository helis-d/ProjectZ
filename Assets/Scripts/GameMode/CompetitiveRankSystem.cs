using UnityEngine;

namespace ProjectZ.GameMode
{
    public enum CompetitiveRankBand
    {
        Baslangic = 0,
        Bronz = 1,
        Gumus = 2,
        Altin = 3,
        Savasci = 4,
        Kral = 5,
        Baron = 6,
        Madersah = 7,
        Prestij = 8
    }

    public readonly struct CompetitiveRankInfo
    {
        public CompetitiveRankBand Band { get; }
        public int Division { get; }
        public int DivisionsInBand { get; }
        public int RatingFloor { get; }
        public int RatingCeilingExclusive { get; }

        public string BandDisplayName => CompetitiveRankSystem.GetBandDisplayName(Band);
        public string DivisionDisplayName => CompetitiveRankSystem.GetDivisionDisplayName(Division);
        public string DisplayName => $"{BandDisplayName} {DivisionDisplayName}";

        public CompetitiveRankInfo(
            CompetitiveRankBand band,
            int division,
            int divisionsInBand,
            int ratingFloor,
            int ratingCeilingExclusive)
        {
            Band = band;
            Division = division;
            DivisionsInBand = divisionsInBand;
            RatingFloor = ratingFloor;
            RatingCeilingExclusive = ratingCeilingExclusive;
        }
    }

    public readonly struct RankedMatchPerformance
    {
        public int PlayerRating { get; }
        public int OpponentAverageRating { get; }
        public bool Won { get; }
        public int Kills { get; }
        public int Deaths { get; }
        public int Assists { get; }
        public int RoundsWon { get; }
        public int RoundsLost { get; }
        public bool WasMvp { get; }
        public int RankedMatchesPlayed { get; }

        public RankedMatchPerformance(
            int playerRating,
            int opponentAverageRating,
            bool won,
            int kills,
            int deaths,
            int assists,
            int roundsWon,
            int roundsLost,
            bool wasMvp,
            int rankedMatchesPlayed)
        {
            PlayerRating = playerRating;
            OpponentAverageRating = opponentAverageRating;
            Won = won;
            Kills = kills;
            Deaths = deaths;
            Assists = assists;
            RoundsWon = roundsWon;
            RoundsLost = roundsLost;
            WasMvp = wasMvp;
            RankedMatchesPlayed = rankedMatchesPlayed;
        }
    }

    public readonly struct RankedProgressionResult
    {
        public int PreviousRating { get; }
        public int NewRating { get; }
        public int Delta => NewRating - PreviousRating;
        public CompetitiveRankInfo PreviousRank { get; }
        public CompetitiveRankInfo NewRank { get; }

        public RankedProgressionResult(int previousRating, int newRating)
        {
            PreviousRating = previousRating;
            NewRating = newRating;
            PreviousRank = CompetitiveRankSystem.GetRankInfo(previousRating);
            NewRank = CompetitiveRankSystem.GetRankInfo(newRating);
        }
    }

    public static class CompetitiveRankSystem
    {
        public const int MinimumRating = 1000;
        public const int StartingRating = MinimumRating;
        public const int DivisionRatingSize = 100;
        public const int PlacementMatches = 10;

        private static readonly RankBandDescriptor[] RankBands =
        {
            new RankBandDescriptor(CompetitiveRankBand.Baslangic, "\u0042a\u015flang\u0131\u00e7", 3),
            new RankBandDescriptor(CompetitiveRankBand.Bronz, "Bronz", 3),
            new RankBandDescriptor(CompetitiveRankBand.Gumus, "G\u00fcm\u00fc\u015f", 3),
            new RankBandDescriptor(CompetitiveRankBand.Altin, "Alt\u0131n", 3),
            new RankBandDescriptor(CompetitiveRankBand.Savasci, "Sava\u015f\u00e7\u0131", 3),
            new RankBandDescriptor(CompetitiveRankBand.Kral, "Kral", 3),
            new RankBandDescriptor(CompetitiveRankBand.Baron, "Baron", 4),
            new RankBandDescriptor(CompetitiveRankBand.Madersah, "Mader\u015fah", 4),
            new RankBandDescriptor(CompetitiveRankBand.Prestij, "Prestij", 4),
        };

        private static readonly int TotalDivisionCount;

        static CompetitiveRankSystem()
        {
            int count = 0;
            for (int i = 0; i < RankBands.Length; i++)
                count += RankBands[i].Divisions;

            TotalDivisionCount = count;
        }

        public static CompetitiveRankInfo GetRankInfo(int rating)
        {
            int normalizedRating = Mathf.Max(MinimumRating, rating);
            int relativeRating = normalizedRating - MinimumRating;
            int divisionIndex = Mathf.Clamp(relativeRating / DivisionRatingSize, 0, TotalDivisionCount - 1);
            int runningDivisions = 0;

            for (int i = 0; i < RankBands.Length; i++)
            {
                RankBandDescriptor band = RankBands[i];
                int bandLimit = runningDivisions + band.Divisions;
                if (divisionIndex < bandLimit)
                {
                    int division = (divisionIndex - runningDivisions) + 1;
                    int floor = MinimumRating + (divisionIndex * DivisionRatingSize);
                    int ceiling = divisionIndex >= TotalDivisionCount - 1
                        ? int.MaxValue
                        : floor + DivisionRatingSize;

                    return new CompetitiveRankInfo(
                        band.Band,
                        division,
                        band.Divisions,
                        floor,
                        ceiling);
                }

                runningDivisions = bandLimit;
            }

            RankBandDescriptor topBand = RankBands[RankBands.Length - 1];
            int topFloor = MinimumRating + ((TotalDivisionCount - 1) * DivisionRatingSize);
            return new CompetitiveRankInfo(topBand.Band, topBand.Divisions, topBand.Divisions, topFloor, int.MaxValue);
        }

        public static int GetFloorRating(CompetitiveRankBand band, int division)
        {
            RankBandDescriptor descriptor = GetDescriptor(band);
            int clampedDivision = Mathf.Clamp(division, 1, descriptor.Divisions);
            int divisionIndex = 0;

            for (int i = 0; i < RankBands.Length; i++)
            {
                if (RankBands[i].Band == band)
                    break;

                divisionIndex += RankBands[i].Divisions;
            }

            divisionIndex += clampedDivision - 1;
            return MinimumRating + (divisionIndex * DivisionRatingSize);
        }

        public static int ApplyRatingDelta(int currentRating, int delta)
        {
            return Mathf.Max(MinimumRating, currentRating + delta);
        }

        public static RankedProgressionResult BuildProgressionResult(int previousRating, int newRating)
        {
            return new RankedProgressionResult(previousRating, ApplyRatingDelta(newRating, 0));
        }

        public static int CalculateRatingDelta(RankedMatchPerformance performance)
        {
            int playerRating = Mathf.Max(MinimumRating, performance.PlayerRating);
            int opponentRating = performance.OpponentAverageRating > 0
                ? Mathf.Max(MinimumRating, performance.OpponentAverageRating)
                : playerRating;

            float expectedScore = 1f / (1f + Mathf.Pow(10f, (opponentRating - playerRating) / 400f));
            float actualScore = performance.Won ? 1f : 0f;
            float kFactor = GetKFactor(playerRating, performance.RankedMatchesPlayed);
            int baseDelta = Mathf.RoundToInt(kFactor * (actualScore - expectedScore));
            int performanceBonus = CalculatePerformanceBonus(performance);

            int delta = baseDelta + performanceBonus;
            if (performance.Won)
            {
                int maxGain = performance.RankedMatchesPlayed < PlacementMatches ? 48 : 38;
                delta = Mathf.Clamp(delta, 10, maxGain);
            }
            else
            {
                int maxLoss = performance.RankedMatchesPlayed < PlacementMatches ? -34 : -28;
                delta = Mathf.Clamp(delta, maxLoss, 0);
            }

            return delta;
        }

        public static string GetBandDisplayName(CompetitiveRankBand band)
        {
            return GetDescriptor(band).DisplayName;
        }

        public static string GetDivisionDisplayName(int division)
        {
            return division switch
            {
                1 => "I",
                2 => "II",
                3 => "III",
                4 => "IV",
                _ => "I"
            };
        }

        private static float GetKFactor(int rating, int rankedMatchesPlayed)
        {
            if (rankedMatchesPlayed < PlacementMatches)
                return 40f;

            if (rating < 1600)
                return 32f;
            if (rating < 2200)
                return 28f;
            if (rating < 2800)
                return 24f;
            if (rating < 3400)
                return 20f;

            return 16f;
        }

        private static int CalculatePerformanceBonus(RankedMatchPerformance performance)
        {
            int roundsPlayed = Mathf.Max(1, performance.RoundsWon + performance.RoundsLost);
            float contribution = performance.Kills + (performance.Assists * 0.65f);
            float expectedContribution = Mathf.Max(3f, roundsPlayed * 0.45f);
            float contributionOffset = Mathf.Clamp(
                (contribution - expectedContribution) / Mathf.Max(4f, roundsPlayed * 0.35f),
                -1f,
                1f);

            float survivalScore;
            if (performance.Deaths <= 0)
            {
                survivalScore = 1f;
            }
            else
            {
                survivalScore = Mathf.Clamp((performance.Kills + (performance.Assists * 0.5f)) / performance.Deaths, 0f, 2f);
            }

            float roundMomentum = Mathf.Clamp((performance.RoundsWon - performance.RoundsLost) / (float)roundsPlayed, -1f, 1f);
            float rawBonus = (contributionOffset * 4f)
                + ((survivalScore - 0.5f) * 4f)
                + (roundMomentum * 3f)
                + (performance.WasMvp ? 2f : 0f);

            int bonus = Mathf.RoundToInt(rawBonus);
            if (!performance.Won)
                bonus = Mathf.Min(bonus, 2);

            return Mathf.Clamp(bonus, -6, 8);
        }

        private static RankBandDescriptor GetDescriptor(CompetitiveRankBand band)
        {
            for (int i = 0; i < RankBands.Length; i++)
            {
                if (RankBands[i].Band == band)
                    return RankBands[i];
            }

            return RankBands[0];
        }

        private readonly struct RankBandDescriptor
        {
            public CompetitiveRankBand Band { get; }
            public string DisplayName { get; }
            public int Divisions { get; }

            public RankBandDescriptor(CompetitiveRankBand band, string displayName, int divisions)
            {
                Band = band;
                DisplayName = displayName;
                Divisions = divisions;
            }
        }
    }
}
