using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ProjectZ.Network
{
    [Serializable]
    public sealed class AuthoritativeMatchResultPayload
    {
        public int version = 1;
        public string matchKey;
        public long issuedAtUnix;
        public string mapId;
        public string gameMode;
        public string playerTeam;
        public string winningTeam;
        public bool won;
        public int attackerRoundsWon;
        public int defenderRoundsWon;
        public int kills;
        public int deaths;
        public int assists;
        public bool wasMvp;
        public string heroId;
        public int matchDurationSeconds;
        public int headshotCount;
        public int wallbangCount;
        public int spherePlantsCount;
        public int sphereDefusesCount;
        public int ultimateActivations;
        public int peakCreditsThisMatch;
        public string mostUsedWeaponId;
        public string signature;
    }

    internal static class AuthoritativeMatchResultSigning
    {
        internal const string SecretEnvironmentVariable = "PROJECTZ_MATCH_RESULT_SECRET";

        public static string ResolveSharedSecret()
        {
            string envSecret = Environment.GetEnvironmentVariable(SecretEnvironmentVariable);
            return string.IsNullOrWhiteSpace(envSecret) ? null : envSecret.Trim();
        }

        public static string BuildCanonicalMessage(AuthoritativeMatchResultPayload payload)
        {
            if (payload == null)
                return string.Empty;

            return string.Join("|", new[]
            {
                payload.version.ToString(CultureInfo.InvariantCulture),
                Normalize(payload.matchKey),
                payload.issuedAtUnix.ToString(CultureInfo.InvariantCulture),
                Normalize(payload.mapId),
                Normalize(payload.gameMode),
                Normalize(payload.playerTeam),
                Normalize(payload.winningTeam),
                payload.won ? "1" : "0",
                payload.attackerRoundsWon.ToString(CultureInfo.InvariantCulture),
                payload.defenderRoundsWon.ToString(CultureInfo.InvariantCulture),
                payload.kills.ToString(CultureInfo.InvariantCulture),
                payload.deaths.ToString(CultureInfo.InvariantCulture),
                payload.assists.ToString(CultureInfo.InvariantCulture),
                payload.wasMvp ? "1" : "0",
                Normalize(payload.heroId),
                payload.matchDurationSeconds.ToString(CultureInfo.InvariantCulture),
                payload.headshotCount.ToString(CultureInfo.InvariantCulture),
                payload.wallbangCount.ToString(CultureInfo.InvariantCulture),
                payload.spherePlantsCount.ToString(CultureInfo.InvariantCulture),
                payload.sphereDefusesCount.ToString(CultureInfo.InvariantCulture),
                payload.ultimateActivations.ToString(CultureInfo.InvariantCulture),
                payload.peakCreditsThisMatch.ToString(CultureInfo.InvariantCulture),
                Normalize(payload.mostUsedWeaponId)
            });
        }

        public static string ComputeSignature(AuthoritativeMatchResultPayload payload, string sharedSecret = null)
        {
            string secret = string.IsNullOrWhiteSpace(sharedSecret) ? ResolveSharedSecret() : sharedSecret;
            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException($"Missing required environment variable: {SecretEnvironmentVariable}");

            string message = BuildCanonicalMessage(payload);

            using HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return ConvertToHex(hash);
        }

        public static bool HasValidBasics(AuthoritativeMatchResultPayload payload)
        {
            return payload != null
                && payload.version > 0
                && !string.IsNullOrWhiteSpace(payload.matchKey)
                && !string.IsNullOrWhiteSpace(payload.gameMode)
                && !string.IsNullOrWhiteSpace(payload.winningTeam)
                && !string.IsNullOrWhiteSpace(payload.playerTeam);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }

        private static string ConvertToHex(byte[] bytes)
        {
            StringBuilder builder = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
                builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));

            return builder.ToString();
        }
    }
}
