const STORAGE_COLLECTION = "player_data";
const STORAGE_KEY_PROFILE = "profile";

const RPC_BACKEND_HEALTH = "projectz_backend_health";
const RPC_GET_PROFILE_STATE = "projectz_get_profile_state";
const RPC_SELECT_HERO = "projectz_select_hero";
const RPC_UNLOCK_HERO = "projectz_unlock_hero";
const RPC_PURCHASE_OFFER = "projectz_purchase_offer";
const RPC_APPLY_RANKED_RESULT = "projectz_apply_ranked_result";
const RPC_SUBMIT_MATCH_TELEMETRY = "projectz_submit_match_telemetry";
const RPC_FINALIZE_SIGNED_MATCH_RESULT = "projectz_finalize_signed_match_result";

const MATCH_HANDLER_ID = "custom_lobby";
const RANKED_LEADERBOARD_ID = "ranked_rating";
const TELEMETRY_COLLECTION = "match_telemetry";
const MATCH_RESULT_RECEIPTS_COLLECTION = "match_result_receipts";
const WALLET_ID_COMMAND_CREDITS = "command_credits";
const WALLET_ID_ZCORE = "z_core";

const STARTING_COMMAND_CREDITS = 1000;
const STARTING_ZCORE = 0;
const DEFAULT_HERO_UNLOCK_PRICE = 600;
const MINIMUM_RATING = 1000;
const PLACEMENT_MATCHES = 10;

const MATCH_SERVER_ADDRESS_ENV = "PROJECTZ_MATCH_SERVER_ADDRESS";
const MATCH_SERVER_PORT_ENV = "PROJECTZ_MATCH_SERVER_PORT";
const ENABLE_ALPHA_ENTITLEMENTS_ENV = "PROJECTZ_ENABLE_ALPHA_ENTITLEMENTS";
const ENABLE_SEASON2_ENV = "PROJECTZ_ENABLE_SEASON2";
const ENABLE_EVENT_CONTENT_ENV = "PROJECTZ_ENABLE_EVENT_CONTENT";
const MATCH_RESULT_SECRET_ENV = "PROJECTZ_MATCH_RESULT_SECRET";
const MATCH_RESULT_MAX_AGE_SECONDS = 900;

const PURCHASE_STATUS_SUCCESS = 0;
const PURCHASE_STATUS_INVALID_PROFILE = 1;
const PURCHASE_STATUS_INVALID_OFFER = 2;
const PURCHASE_STATUS_ALREADY_OWNED = 3;
const PURCHASE_STATUS_INSUFFICIENT_FUNDS = 4;
const PURCHASE_STATUS_OFFER_NOT_ACTIVE = 5;
const PURCHASE_STATUS_UNKNOWN_HERO = 6;

const CURRENCY_NONE = 0;
const CURRENCY_COMMAND_CREDITS = 1;
const CURRENCY_ZCORE = 2;

const AVAILABILITY_LAUNCH = 0;
const AVAILABILITY_ALPHA_ONLY = 1;
const AVAILABILITY_SEASON2 = 2;
const AVAILABILITY_EVENT = 3;

const OFFER_TYPE_HERO_UNLOCK = 0;
const OFFER_TYPE_WEAPON_SKIN = 1;
const OFFER_TYPE_PLAYER_CARD = 2;
const OFFER_TYPE_SPRAY = 3;
const OFFER_TYPE_CHARM = 4;
const OFFER_TYPE_BUNDLE = 5;
const OFFER_TYPE_ALPHA_FOUNDER_PACK = 6;
const OFFER_TYPE_BATTLE_PASS = 7;

const STARTER_HERO_IDS = ["volt", "jacob", "silvia", "sai", "helix"];
const ALL_HERO_IDS = ["volt", "jacob", "silvia", "sai", "helix", "lagrange", "sentinel", "sector", "samuel", "jielda", "zauhll", "kant", "marcus20"];

const HERO_DISPLAY_NAMES: {[key: string]: string} = {
    volt: "Volt",
    jacob: "Jacob",
    silvia: "Silvia",
    sai: "Sai",
    helix: "Helix",
    lagrange: "Lagrange",
    sentinel: "Sentinel",
    sector: "Sector",
    samuel: "Samuel",
    jielda: "Jielda",
    zauhll: "Zauhll",
    kant: "Kant",
    marcus20: "Marcus 20"
};

interface LobbyMatchState extends nkruntime.MatchState {
    ip: string;
    port: number;
    allowedUserIds: string[];
}

interface PlayerProfileData {
    displayName: string;
    currency: number;
    commandCredits: number;
    zCore: number;
    elo: number;
    peakElo: number;
    rankedMatchesPlayed: number;
    rankedWins: number;
    rankedLosses: number;
    selectedHero: string;
    primaryWeaponId: string;
    secondaryWeaponId: string;
    meleeWeaponId: string;
    ownedHeroIds: string[];
    ownedCosmeticIds: string[];
    ownedOfferIds: string[];
    weaponMastery: {[key: string]: number};
}

interface CatalogOffer {
    offerId: string;
    displayName: string;
    offerType: number;
    priceCurrency: number;
    price: number;
    contentId: string;
    availability: number;
}

interface RankedResultPayload {
    opponentAverageRating: number;
    won: boolean;
    kills: number;
    deaths: number;
    assists: number;
    roundsWon: number;
    roundsLost: number;
    wasMvp: boolean;
}

interface SignedMatchResultPayload {
    version: number;
    matchKey: string;
    issuedAtUnix: number;
    mapId: string;
    gameMode: string;
    playerTeam: string;
    winningTeam: string;
    won: boolean;
    attackerRoundsWon: number;
    defenderRoundsWon: number;
    kills: number;
    deaths: number;
    assists: number;
    wasMvp: boolean;
    heroId: string;
    matchDurationSeconds: number;
    headshotCount: number;
    wallbangCount: number;
    spherePlantsCount: number;
    sphereDefusesCount: number;
    ultimateActivations: number;
    peakCreditsThisMatch: number;
    mostUsedWeaponId: string;
    signature: string;
}

interface MatchResultReceipt {
    matchKey: string;
    previousRating: number;
    newRating: number;
    delta: number;
    telemetrySaved: boolean;
    processedAtUnix: number;
}

interface WalletState {
    commandCredits: number;
    zCore: number;
}

interface RankedResponsePayload {
    succeeded: boolean;
    errorCode: string | null;
    message: string;
    profile: PlayerProfileData | null;
    previousRating: number;
    newRating: number;
    delta: number;
    persist: boolean;
}

interface SignedMatchResultResponsePayload extends RankedResponsePayload {
    matchKey: string | null;
    telemetrySaved: boolean;
    alreadyProcessed: boolean;
}

let InitModule: nkruntime.InitModule = function(ctx, logger, nk, initializer) {
    logger.info("Initializing ProjectZ authoritative backend module.");
    ensureRankedLeaderboard(logger, nk);
    initializer.registerMatch<LobbyMatchState>(MATCH_HANDLER_ID, {
        matchInit: MatchInit,
        matchJoinAttempt: MatchJoinAttempt,
        matchJoin: MatchJoin,
        matchLeave: MatchLeave,
        matchLoop: MatchLoop,
        matchSignal: MatchSignal,
        matchTerminate: MatchTerminate
    });
    initializer.registerMatchmakerMatched(MatchmakerMatched);
    initializer.registerRpc(RPC_BACKEND_HEALTH, RpcBackendHealth);
    initializer.registerRpc(RPC_GET_PROFILE_STATE, RpcGetProfileState);
    initializer.registerRpc(RPC_SELECT_HERO, RpcSelectHero);
    initializer.registerRpc(RPC_UNLOCK_HERO, RpcUnlockHero);
    initializer.registerRpc(RPC_PURCHASE_OFFER, RpcPurchaseOffer);
    initializer.registerRpc(RPC_APPLY_RANKED_RESULT, RpcApplyRankedResult);
    initializer.registerRpc(RPC_SUBMIT_MATCH_TELEMETRY, RpcSubmitMatchTelemetry);
    initializer.registerRpc(RPC_FINALIZE_SIGNED_MATCH_RESULT, RpcFinalizeSignedMatchResult);
};

function MatchmakerMatched(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, matches: nkruntime.MatchmakerResult[]): string {
    const userIds = matches.map(function(match) { return match.presence.userId; });
    return nk.matchCreate(MATCH_HANDLER_ID, {
        ip: readStringEnv(ctx, MATCH_SERVER_ADDRESS_ENV, "127.0.0.1"),
        port: readNumberEnv(ctx, MATCH_SERVER_PORT_ENV, 7770),
        allowedUserIds: userIds
    });
}

function MatchInit(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, params: {[key: string]: any}) {
    const state: LobbyMatchState = {
        ip: stringifyValue(params.ip, "127.0.0.1"),
        port: readNumber(params.port, 7770),
        allowedUserIds: normalizeIds(params.allowedUserIds)
    };
    return {
        state: state,
        tickRate: 1,
        label: JSON.stringify({ mode: "session_bridge", ip: state.ip, port: state.port, reservedCount: state.allowedUserIds.length })
    };
}

function MatchJoinAttempt(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, dispatcher: nkruntime.MatchDispatcher, tick: number, state: LobbyMatchState, presence: nkruntime.Presence) {
    if (!containsString(state.allowedUserIds, presence.userId)) {
        return { state: state, accept: false, rejectMessage: "match reservation missing" };
    }
    return { state: state, accept: true };
}

function MatchJoin(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, dispatcher: nkruntime.MatchDispatcher, tick: number, state: LobbyMatchState, presences: nkruntime.Presence[]) {
    dispatcher.broadcastMessage(1, JSON.stringify({ ip: state.ip, port: state.port }), presences, null, true);
    return { state: state };
}

function MatchLeave(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, dispatcher: nkruntime.MatchDispatcher, tick: number, state: LobbyMatchState) {
    return { state: state };
}

function MatchLoop(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, dispatcher: nkruntime.MatchDispatcher, tick: number, state: LobbyMatchState) {
    return { state: state };
}

function MatchSignal(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, dispatcher: nkruntime.MatchDispatcher, tick: number, state: LobbyMatchState, data: string) {
    return { state: state, data: data };
}

function MatchTerminate(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, dispatcher: nkruntime.MatchDispatcher, tick: number, state: LobbyMatchState) {
    return { state: state };
}

function RpcBackendHealth(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama): string {
    return JSON.stringify({ succeeded: true, message: "ProjectZ backend online.", backendVersion: "phase3", rankedLeaderboardId: RANKED_LEADERBOARD_ID });
}

function RpcGetProfileState(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama): string {
    return runProfileRpc(ctx, logger, nk, function(userId, profile) {
        return response(true, null, "Profile state loaded.", profile, null, false);
    });
}

function RpcSelectHero(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, payload: string): string {
    return runProfileRpc(ctx, logger, nk, function(userId, profile) {
        const request = parsePayload(payload);
        const heroId = normalizeId(request.heroId);
        if (!isKnownHero(heroId)) {
            return response(false, "unknown_hero", "Unknown hero selection.", profile);
        }
        if (!ownsHero(profile, heroId)) {
            return response(false, "hero_locked", "Hero is not owned.", profile);
        }
        profile.selectedHero = heroId;
        return response(true, null, "Selected hero updated.", profile, null, true);
    });
}

function RpcUnlockHero(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, payload: string): string {
    return runProfileRpc(ctx, logger, nk, function(userId, profile) {
        const request = parsePayload(payload);
        const purchase = tryUnlockHero(userId, nk, profile, normalizeId(request.heroId));
        return response(purchase.status === PURCHASE_STATUS_SUCCESS, purchaseStatusToCode(purchase.status), purchase.message, profile, purchase, purchase.status === PURCHASE_STATUS_SUCCESS);
    });
}

function RpcPurchaseOffer(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, payload: string): string {
    return runProfileRpc(ctx, logger, nk, function(userId, profile) {
        const request = parsePayload(payload);
        const purchase = tryPurchaseOffer(ctx, userId, nk, profile, getOfferById(normalizeId(request.offerId)));
        return response(purchase.status === PURCHASE_STATUS_SUCCESS, purchaseStatusToCode(purchase.status), purchase.message, profile, purchase, purchase.status === PURCHASE_STATUS_SUCCESS);
    });
}

function RpcApplyRankedResult(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, payload: string): string {
    return runProfileRpc(ctx, logger, nk, function(userId, profile) {
        const request = parsePayload(payload);
        const performance = buildRankedResultPayload(request, profile);
        const previousRating = profile.elo;
        const delta = calculateRankedRatingDelta(performance, previousRating, profile.rankedMatchesPlayed);
        const newRating = applyRankedRatingDelta(previousRating, delta);

        profile.elo = newRating;
        profile.rankedMatchesPlayed += 1;
        if (performance.won) {
            profile.rankedWins += 1;
        } else {
            profile.rankedLosses += 1;
        }
        if (profile.peakElo < newRating) {
            profile.peakElo = newRating;
        }

        try {
            nk.leaderboardRecordWrite(
                RANKED_LEADERBOARD_ID,
                userId,
                ctx.username || profile.displayName,
                newRating,
                profile.peakElo,
                {
                    delta: delta,
                    won: performance.won,
                    opponentAverageRating: performance.opponentAverageRating,
                    rankedMatchesPlayed: profile.rankedMatchesPlayed
                },
                nkruntime.OverrideOperator.SET);
        } catch (error) {
            logger.info("Ranked leaderboard write skipped: " + errorToString(error));
        }

        return rankedResponse({
            succeeded: true,
            errorCode: null,
            message: "Ranked result persisted.",
            profile: profile,
            previousRating: previousRating,
            newRating: newRating,
            delta: delta,
            persist: true
        });
    });
}

function RpcSubmitMatchTelemetry(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, payload: string): string {
    if (!ctx.userId) {
        return JSON.stringify(telemetryResponse(false, "unauthorized", "Authentication required.", null));
    }

    try {
        const request = parsePayload(payload);
        const matchKey = nk.uuidv4().replace(/-/g, "");
        const timestampUnix = Math.floor(Date.now() / 1000);
        const telemetry = {
            user_id: ctx.userId,
            match_key: matchKey,
            timestamp_unix: timestampUnix,
            map_id: stringifyValue(request.mapId, "unknown_map"),
            game_mode: stringifyValue(request.gameMode, "unknown_mode"),
            winning_team: stringifyValue(request.winningTeam, "unknown_team"),
            match_duration_sec: clampMin(readNumber(request.matchDurationSeconds, 0), 0),
            total_rounds: clampMin(readNumber(request.totalRoundsPlayed, 0), 0),
            attacker_rounds_won: clampMin(readNumber(request.attackerRoundsWon, 0), 0),
            defender_rounds_won: clampMin(readNumber(request.defenderRoundsWon, 0), 0),
            kills: clampMin(readNumber(request.kills, 0), 0),
            deaths: clampMin(readNumber(request.deaths, 0), 0),
            assists: clampMin(readNumber(request.assists, 0), 0),
            headshot_count: clampMin(readNumber(request.headshotCount, 0), 0),
            wallbang_count: clampMin(readNumber(request.wallbangCount, 0), 0),
            was_mvp: !!request.wasMvp,
            hero_id: normalizeId(request.heroId),
            most_used_weapon_id: normalizeId(request.mostUsedWeaponId),
            sphere_plants: clampMin(readNumber(request.spherePlantsCount, 0), 0),
            sphere_defuses: clampMin(readNumber(request.sphereDefusesCount, 0), 0),
            ultimate_activations: clampMin(readNumber(request.ultimateActivations, 0), 0),
            elo_before: clampMin(readNumber(request.eloBefore, MINIMUM_RATING), MINIMUM_RATING),
            elo_delta: readNumber(request.eloDelta, 0),
            peak_credits_this_match: clampMin(readNumber(request.peakCreditsThisMatch, 0), 0)
        };

        nk.storageWrite([{
            collection: TELEMETRY_COLLECTION,
            key: matchKey,
            userId: ctx.userId,
            value: telemetry,
            permissionRead: 0,
            permissionWrite: 0
        }]);

        return JSON.stringify(telemetryResponse(true, null, "Match telemetry saved.", matchKey));
    } catch (error) {
        logger.error("Telemetry RPC failed: " + errorToString(error));
        return JSON.stringify(telemetryResponse(false, "server_error", "Telemetry write failed.", null));
    }
}

function RpcFinalizeSignedMatchResult(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, payload: string): string {
    if (!ctx.userId) {
        return JSON.stringify(signedMatchResultResponse({
            succeeded: false,
            errorCode: "unauthorized",
            message: "Authentication required.",
            profile: null,
            previousRating: 0,
            newRating: 0,
            delta: 0,
            matchKey: null,
            telemetrySaved: false,
            alreadyProcessed: false,
            persist: false
        }));
    }

    try {
        const request = buildSignedMatchResultPayload(parsePayload(payload));
        const validationError = validateSignedMatchResultPayload(ctx, nk, request);
        if (validationError) {
            return JSON.stringify(signedMatchResultResponse({
                succeeded: false,
                errorCode: validationError.code,
                message: validationError.message,
                profile: null,
                previousRating: 0,
                newRating: 0,
                delta: 0,
                matchKey: request.matchKey,
                telemetrySaved: false,
                alreadyProcessed: false,
                persist: false
            }));
        }

        const loaded = loadOrCreateProfile(ctx.userId, ctx.username, nk);
        const existingReceipt = readMatchResultReceipt(ctx.userId, request.matchKey, nk);
        if (existingReceipt) {
            return JSON.stringify(signedMatchResultResponse({
                succeeded: true,
                errorCode: null,
                message: "Signed match result already processed.",
                profile: loaded.profile,
                previousRating: existingReceipt.previousRating,
                newRating: existingReceipt.newRating,
                delta: existingReceipt.delta,
                matchKey: existingReceipt.matchKey,
                telemetrySaved: existingReceipt.telemetrySaved,
                alreadyProcessed: true,
                persist: true
            }));
        }

        let previousRating = loaded.profile.elo;
        let newRating = previousRating;
        let delta = 0;

        if (request.gameMode === "ranked") {
            const performance = signedPayloadToRankedResult(request, loaded.profile);
            delta = calculateRankedRatingDelta(performance, previousRating, loaded.profile.rankedMatchesPlayed);
            newRating = applyRankedRatingDelta(previousRating, delta);

            loaded.profile.elo = newRating;
            loaded.profile.rankedMatchesPlayed += 1;
            if (performance.won) {
                loaded.profile.rankedWins += 1;
            } else {
                loaded.profile.rankedLosses += 1;
            }
            if (loaded.profile.peakElo < newRating) {
                loaded.profile.peakElo = newRating;
            }

            try {
                nk.leaderboardRecordWrite(
                    RANKED_LEADERBOARD_ID,
                    ctx.userId,
                    ctx.username || loaded.profile.displayName,
                    newRating,
                    loaded.profile.peakElo,
                    {
                        delta: delta,
                        won: performance.won,
                        opponentAverageRating: performance.opponentAverageRating,
                        rankedMatchesPlayed: loaded.profile.rankedMatchesPlayed,
                        matchKey: request.matchKey
                    },
                    nkruntime.OverrideOperator.SET);
            } catch (error) {
                logger.info("Signed ranked leaderboard write skipped: " + errorToString(error));
            }
        }

        const telemetrySaved = writeSignedTelemetry(ctx.userId, logger, nk, request, previousRating, delta);
        writeProfile(ctx.userId, loaded.profile, loaded.version, nk);
        writeMatchResultReceipt(ctx.userId, request.matchKey, nk, {
            matchKey: request.matchKey,
            previousRating: previousRating,
            newRating: newRating,
            delta: delta,
            telemetrySaved: telemetrySaved,
            processedAtUnix: Math.floor(Date.now() / 1000)
        });

        return JSON.stringify(signedMatchResultResponse({
            succeeded: true,
            errorCode: null,
            message: "Signed match result applied.",
            profile: loaded.profile,
            previousRating: previousRating,
            newRating: newRating,
            delta: delta,
            matchKey: request.matchKey,
            telemetrySaved: telemetrySaved,
            alreadyProcessed: false,
            persist: true
        }));
    } catch (error) {
        logger.error("Signed match result RPC failed: " + errorToString(error));
        return JSON.stringify(signedMatchResultResponse({
            succeeded: false,
            errorCode: "server_error",
            message: "Signed match result validation failed.",
            profile: null,
            previousRating: 0,
            newRating: 0,
            delta: 0,
            matchKey: null,
            telemetrySaved: false,
            alreadyProcessed: false,
            persist: false
        }));
    }
}

function runProfileRpc(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, handler: (userId: string, profile: PlayerProfileData) => any): string {
    const userId = ctx.userId;
    if (!userId) {
        return JSON.stringify(response(false, "unauthorized", "Authentication required.", null));
    }
    try {
        const loaded = loadOrCreateProfile(userId, ctx.username, nk);
        const result = handler(userId, loaded.profile);
        if (result.persist) {
            writeProfile(userId, loaded.profile, loaded.version, nk);
        }
        delete result.persist;
        return JSON.stringify(result);
    } catch (error) {
        logger.error("Profile RPC failed: " + errorToString(error));
        return JSON.stringify(response(false, "server_error", "Backend operation failed.", null));
    }
}

function loadOrCreateProfile(userId: string, username: string | undefined, nk: nkruntime.Nakama) {
    const objects = nk.storageRead([{ collection: STORAGE_COLLECTION, key: STORAGE_KEY_PROFILE, userId: userId }]);
    if (objects && objects.length > 0) {
        const existingProfile = sanitizeProfile(castProfile(objects[0].value));
        applyWalletStateToProfile(existingProfile, ensureWalletState(userId, existingProfile, nk));
        return { profile: existingProfile, version: objects[0].version };
    }
    const profile = createDefaultProfile(username || "NewPlayer");
    applyWalletStateToProfile(profile, ensureWalletState(userId, profile, nk));
    writeProfile(userId, profile, null, nk);
    return { profile: profile, version: null };
}

function writeProfile(userId: string, profile: PlayerProfileData, version: string | null, nk: nkruntime.Nakama): void {
    const write: nkruntime.StorageWriteRequest = {
        collection: STORAGE_COLLECTION,
        key: STORAGE_KEY_PROFILE,
        userId: userId,
        value: sanitizeProfile(profile),
        permissionRead: 1,
        permissionWrite: 1
    };
    if (version) {
        write.version = version;
    }
    nk.storageWrite([write]);
}

function createDefaultProfile(username: string): PlayerProfileData {
    return sanitizeProfile({
        displayName: username || "NewPlayer",
        currency: STARTING_COMMAND_CREDITS,
        commandCredits: STARTING_COMMAND_CREDITS,
        zCore: STARTING_ZCORE,
        elo: 1000,
        peakElo: 1000,
        rankedMatchesPlayed: 0,
        rankedWins: 0,
        rankedLosses: 0,
        selectedHero: "volt",
        primaryWeaponId: "vandal",
        secondaryWeaponId: "pistol_classic",
        meleeWeaponId: "knife_tactical",
        ownedHeroIds: STARTER_HERO_IDS.slice(0),
        ownedCosmeticIds: [],
        ownedOfferIds: [],
        weaponMastery: {}
    });
}

function castProfile(raw: {[key: string]: any}): PlayerProfileData {
    return {
        displayName: stringifyValue(raw.displayName, "NewPlayer"),
        currency: readNumber(raw.currency, STARTING_COMMAND_CREDITS),
        commandCredits: readNumber(raw.commandCredits, readNumber(raw.currency, STARTING_COMMAND_CREDITS)),
        zCore: readNumber(raw.zCore, STARTING_ZCORE),
        elo: readNumber(raw.elo, 1000),
        peakElo: readNumber(raw.peakElo, 1000),
        rankedMatchesPlayed: readNumber(raw.rankedMatchesPlayed, 0),
        rankedWins: readNumber(raw.rankedWins, 0),
        rankedLosses: readNumber(raw.rankedLosses, 0),
        selectedHero: stringifyValue(raw.selectedHero, "volt"),
        primaryWeaponId: stringifyValue(raw.primaryWeaponId, "vandal"),
        secondaryWeaponId: stringifyValue(raw.secondaryWeaponId, "pistol_classic"),
        meleeWeaponId: stringifyValue(raw.meleeWeaponId, "knife_tactical"),
        ownedHeroIds: normalizeIds(raw.ownedHeroIds),
        ownedCosmeticIds: normalizeIds(raw.ownedCosmeticIds),
        ownedOfferIds: normalizeIds(raw.ownedOfferIds),
        weaponMastery: typeof raw.weaponMastery === "object" && raw.weaponMastery ? raw.weaponMastery : {}
    };
}

function sanitizeProfile(profile: PlayerProfileData): PlayerProfileData {
    profile.displayName = stringifyValue(profile.displayName, "NewPlayer");
    profile.commandCredits = clampMin(readNumber(profile.commandCredits, STARTING_COMMAND_CREDITS), 0);
    profile.currency = profile.commandCredits;
    profile.zCore = clampMin(readNumber(profile.zCore, STARTING_ZCORE), 0);
    profile.elo = clampMin(readNumber(profile.elo, 1000), 1000);
    profile.peakElo = clampMin(readNumber(profile.peakElo, profile.elo), profile.elo);
    profile.rankedMatchesPlayed = clampMin(readNumber(profile.rankedMatchesPlayed, 0), 0);
    profile.rankedWins = clampMin(readNumber(profile.rankedWins, 0), 0);
    profile.rankedLosses = clampMin(readNumber(profile.rankedLosses, 0), 0);
    profile.primaryWeaponId = stringifyValue(profile.primaryWeaponId, "vandal");
    profile.secondaryWeaponId = stringifyValue(profile.secondaryWeaponId, "pistol_classic");
    profile.meleeWeaponId = stringifyValue(profile.meleeWeaponId, "knife_tactical");
    profile.ownedHeroIds = normalizeIds(profile.ownedHeroIds);
    profile.ownedCosmeticIds = normalizeIds(profile.ownedCosmeticIds);
    profile.ownedOfferIds = normalizeIds(profile.ownedOfferIds);
    for (const starterHeroId of STARTER_HERO_IDS) {
        if (!containsString(profile.ownedHeroIds, starterHeroId)) {
            profile.ownedHeroIds.push(starterHeroId);
        }
    }
    profile.selectedHero = ownsHero(profile, normalizeId(profile.selectedHero)) ? normalizeId(profile.selectedHero) : resolveDefaultHero(profile);
    return profile;
}

function tryUnlockHero(userId: string, nk: nkruntime.Nakama, profile: PlayerProfileData, heroId: string) {
    if (!isKnownHero(heroId)) {
        return purchase(PURCHASE_STATUS_UNKNOWN_HERO, "hero_unlock_" + heroId, heroId, "Unknown hero unlock request.", CURRENCY_NONE, 0);
    }
    if (ownsHero(profile, heroId)) {
        return purchase(PURCHASE_STATUS_ALREADY_OWNED, "hero_unlock_" + heroId, heroId, "Hero already owned.", CURRENCY_NONE, 0);
    }
    const walletSpend = spendWalletCurrency(userId, nk, CURRENCY_COMMAND_CREDITS, DEFAULT_HERO_UNLOCK_PRICE, {
        reason: "hero_unlock",
        heroId: heroId
    });
    if (!walletSpend.succeeded) {
        return purchase(PURCHASE_STATUS_INSUFFICIENT_FUNDS, "hero_unlock_" + heroId, heroId, "Not enough Command Credits.", CURRENCY_COMMAND_CREDITS, 0);
    }
    applyWalletStateToProfile(profile, walletSpend.wallet);
    profile.ownedHeroIds.push(heroId);
    profile.ownedHeroIds = normalizeIds(profile.ownedHeroIds);
    return purchase(PURCHASE_STATUS_SUCCESS, "hero_unlock_" + heroId, heroId, getHeroDisplayName(heroId) + " unlocked permanently.", CURRENCY_COMMAND_CREDITS, DEFAULT_HERO_UNLOCK_PRICE);
}

function tryPurchaseOffer(ctx: nkruntime.Context, userId: string, nk: nkruntime.Nakama, profile: PlayerProfileData, offer: CatalogOffer | null) {
    if (!offer) {
        return purchase(PURCHASE_STATUS_INVALID_OFFER, null, null, "Offer not found.", CURRENCY_NONE, 0);
    }
    if (!isOfferActive(ctx, offer)) {
        return purchase(PURCHASE_STATUS_OFFER_NOT_ACTIVE, offer.offerId, offer.contentId, "Offer is not currently active.", CURRENCY_NONE, 0);
    }
    if (offer.offerType === OFFER_TYPE_HERO_UNLOCK) {
        return tryUnlockHero(userId, nk, profile, normalizeId(offer.contentId));
    }
    if (ownsOffer(profile, offer)) {
        return purchase(PURCHASE_STATUS_ALREADY_OWNED, offer.offerId, offer.contentId, "Offer already owned.", CURRENCY_NONE, 0);
    }
    const walletSpend = spendWalletCurrency(userId, nk, offer.priceCurrency, offer.price, {
        reason: "offer_purchase",
        offerId: offer.offerId,
        contentId: offer.contentId
    });
    if (!walletSpend.succeeded) {
        return purchase(PURCHASE_STATUS_INSUFFICIENT_FUNDS, offer.offerId, offer.contentId, "Insufficient funds.", offer.priceCurrency, 0);
    }
    applyWalletStateToProfile(profile, walletSpend.wallet);
    grantOffer(profile, offer);
    return purchase(PURCHASE_STATUS_SUCCESS, offer.offerId, offer.contentId, offer.displayName + " purchased successfully.", offer.priceCurrency, offer.price);
}

function getOfferById(offerId: string): CatalogOffer | null {
    const offers = getCatalogOffers();
    for (const offer of offers) {
        if (normalizeId(offer.offerId) === offerId) {
            return offer;
        }
    }
    return null;
}

function getCatalogOffers(): CatalogOffer[] {
    const offers: CatalogOffer[] = [];
    for (const heroId of ALL_HERO_IDS) {
        if (!containsString(STARTER_HERO_IDS, heroId)) {
            offers.push({ offerId: "hero_unlock_" + heroId, displayName: getHeroDisplayName(heroId) + " Hero Unlock", offerType: OFFER_TYPE_HERO_UNLOCK, priceCurrency: CURRENCY_COMMAND_CREDITS, price: DEFAULT_HERO_UNLOCK_PRICE, contentId: heroId, availability: AVAILABILITY_LAUNCH });
        }
    }
    offers.push(
        { offerId: "launch_weaponskin_vandal_firstlight", displayName: "Firstlight Vandal", offerType: OFFER_TYPE_WEAPON_SKIN, priceCurrency: CURRENCY_ZCORE, price: 900, contentId: "weaponskin_vandal_firstlight", availability: AVAILABILITY_LAUNCH },
        { offerId: "launch_playercard_founders_signal", displayName: "Founder's Signal Card", offerType: OFFER_TYPE_PLAYER_CARD, priceCurrency: CURRENCY_ZCORE, price: 250, contentId: "playercard_founders_signal", availability: AVAILABILITY_LAUNCH },
        { offerId: "launch_spray_hold_the_site", displayName: "Hold The Site Spray", offerType: OFFER_TYPE_SPRAY, priceCurrency: CURRENCY_ZCORE, price: 175, contentId: "spray_hold_the_site", availability: AVAILABILITY_LAUNCH },
        { offerId: "launch_charm_quantum_key", displayName: "Quantum Key Charm", offerType: OFFER_TYPE_CHARM, priceCurrency: CURRENCY_ZCORE, price: 225, contentId: "charm_quantum_key", availability: AVAILABILITY_LAUNCH },
        { offerId: "alpha_founder_pack", displayName: "Alpha Founder Pack", offerType: OFFER_TYPE_ALPHA_FOUNDER_PACK, priceCurrency: CURRENCY_ZCORE, price: 1800, contentId: "alpha_founder_pack", availability: AVAILABILITY_ALPHA_ONLY },
        { offerId: "season2_battlepass_premium", displayName: "Season 2 Battle Pass", offerType: OFFER_TYPE_BATTLE_PASS, priceCurrency: CURRENCY_ZCORE, price: 1000, contentId: "season2_battlepass_premium", availability: AVAILABILITY_SEASON2 }
    );
    return offers;
}

function grantOffer(profile: PlayerProfileData, offer: CatalogOffer): void {
    if (offer.offerType === OFFER_TYPE_WEAPON_SKIN || offer.offerType === OFFER_TYPE_PLAYER_CARD || offer.offerType === OFFER_TYPE_SPRAY || offer.offerType === OFFER_TYPE_CHARM) {
        profile.ownedCosmeticIds.push(normalizeId(offer.contentId));
        profile.ownedCosmeticIds = normalizeIds(profile.ownedCosmeticIds);
        return;
    }
    profile.ownedOfferIds.push(normalizeId(offer.offerId));
    profile.ownedOfferIds = normalizeIds(profile.ownedOfferIds);
}

function ownsOffer(profile: PlayerProfileData, offer: CatalogOffer): boolean {
    if (offer.offerType === OFFER_TYPE_HERO_UNLOCK) {
        return ownsHero(profile, normalizeId(offer.contentId));
    }
    if (offer.offerType === OFFER_TYPE_WEAPON_SKIN || offer.offerType === OFFER_TYPE_PLAYER_CARD || offer.offerType === OFFER_TYPE_SPRAY || offer.offerType === OFFER_TYPE_CHARM) {
        return containsString(profile.ownedCosmeticIds, normalizeId(offer.contentId));
    }
    return containsString(profile.ownedOfferIds, normalizeId(offer.offerId));
}

function isOfferActive(ctx: nkruntime.Context, offer: CatalogOffer): boolean {
    if (offer.availability === AVAILABILITY_LAUNCH) return true;
    if (offer.availability === AVAILABILITY_ALPHA_ONLY) return readBoolEnv(ctx, ENABLE_ALPHA_ENTITLEMENTS_ENV, false);
    if (offer.availability === AVAILABILITY_SEASON2) return readBoolEnv(ctx, ENABLE_SEASON2_ENV, false);
    if (offer.availability === AVAILABILITY_EVENT) return readBoolEnv(ctx, ENABLE_EVENT_CONTENT_ENV, false);
    return false;
}

function spendWalletCurrency(userId: string, nk: nkruntime.Nakama, currencyType: number, amount: number, metadata?: {[key: string]: any}) {
    if (amount <= 0) {
        return { succeeded: true, wallet: readWalletState(userId, nk) };
    }

    const walletKey = getWalletId(currencyType);
    if (!walletKey) {
        return { succeeded: false, wallet: readWalletState(userId, nk) };
    }

    const changeset: {[key: string]: number} = {};
    changeset[walletKey] = -Math.abs(amount);

    try {
        const walletResult = nk.walletUpdate(userId, changeset, metadata || {}, true);
        return { succeeded: true, wallet: walletStateFromRaw(walletResult.updated) };
    } catch (error) {
        return { succeeded: false, wallet: readWalletState(userId, nk), error: errorToString(error) };
    }
}

function response(succeeded: boolean, errorCode: string | null, message: string, profile: PlayerProfileData | null, purchaseResult?: any, persist?: boolean) {
    return { succeeded: succeeded, errorCode: errorCode, message: message, profile: profile, purchase: purchaseResult || null, persist: !!persist };
}

function rankedResponse(payload: RankedResponsePayload) {
    return {
        succeeded: payload.succeeded,
        errorCode: payload.errorCode,
        message: payload.message,
        profile: payload.profile,
        previousRating: payload.previousRating,
        newRating: payload.newRating,
        delta: payload.delta,
        persist: payload.persist
    };
}

function telemetryResponse(succeeded: boolean, errorCode: string | null, message: string, matchKey: string | null) {
    return {
        succeeded: succeeded,
        errorCode: errorCode,
        message: message,
        matchKey: matchKey
    };
}

function signedMatchResultResponse(payload: SignedMatchResultResponsePayload) {
    return {
        succeeded: payload.succeeded,
        errorCode: payload.errorCode,
        message: payload.message,
        profile: payload.profile,
        previousRating: payload.previousRating,
        newRating: payload.newRating,
        delta: payload.delta,
        matchKey: payload.matchKey,
        telemetrySaved: payload.telemetrySaved,
        alreadyProcessed: payload.alreadyProcessed
    };
}

function purchase(status: number, offerId: string | null, contentId: string | null, message: string, currencyType: number, amountSpent: number) {
    return { status: status, offerId: offerId, contentId: contentId, message: message, currencyType: currencyType, amountSpent: amountSpent };
}

function purchaseStatusToCode(status: number): string | null {
    if (status === PURCHASE_STATUS_SUCCESS) return null;
    if (status === PURCHASE_STATUS_INVALID_PROFILE) return "invalid_profile";
    if (status === PURCHASE_STATUS_INVALID_OFFER) return "invalid_offer";
    if (status === PURCHASE_STATUS_ALREADY_OWNED) return "already_owned";
    if (status === PURCHASE_STATUS_INSUFFICIENT_FUNDS) return "insufficient_funds";
    if (status === PURCHASE_STATUS_OFFER_NOT_ACTIVE) return "offer_not_active";
    if (status === PURCHASE_STATUS_UNKNOWN_HERO) return "unknown_hero";
    return "unknown_error";
}

function ensureRankedLeaderboard(logger: nkruntime.Logger, nk: nkruntime.Nakama): void {
    try {
        nk.leaderboardCreate(RANKED_LEADERBOARD_ID, true, nkruntime.SortOrder.DESCENDING, nkruntime.Operator.SET, null, { mode: "ranked" }, true);
    } catch (error) {
        logger.info("Ranked leaderboard create skipped: " + errorToString(error));
    }
}

function buildSignedMatchResultPayload(raw: any): SignedMatchResultPayload {
    return {
        version: clampMin(readNumber(raw.version, 1), 1),
        matchKey: normalizeId(raw.matchKey),
        issuedAtUnix: clampMin(readNumber(raw.issuedAtUnix, 0), 0),
        mapId: normalizeId(raw.mapId),
        gameMode: normalizeId(raw.gameMode),
        playerTeam: normalizeId(raw.playerTeam),
        winningTeam: normalizeId(raw.winningTeam),
        won: !!raw.won,
        attackerRoundsWon: clampMin(readNumber(raw.attackerRoundsWon, 0), 0),
        defenderRoundsWon: clampMin(readNumber(raw.defenderRoundsWon, 0), 0),
        kills: clampMin(readNumber(raw.kills, 0), 0),
        deaths: clampMin(readNumber(raw.deaths, 0), 0),
        assists: clampMin(readNumber(raw.assists, 0), 0),
        wasMvp: !!raw.wasMvp,
        heroId: normalizeId(raw.heroId),
        matchDurationSeconds: clampMin(readNumber(raw.matchDurationSeconds, 0), 0),
        headshotCount: clampMin(readNumber(raw.headshotCount, 0), 0),
        wallbangCount: clampMin(readNumber(raw.wallbangCount, 0), 0),
        spherePlantsCount: clampMin(readNumber(raw.spherePlantsCount, 0), 0),
        sphereDefusesCount: clampMin(readNumber(raw.sphereDefusesCount, 0), 0),
        ultimateActivations: clampMin(readNumber(raw.ultimateActivations, 0), 0),
        peakCreditsThisMatch: clampMin(readNumber(raw.peakCreditsThisMatch, 0), 0),
        mostUsedWeaponId: normalizeId(raw.mostUsedWeaponId),
        signature: normalizeId(raw.signature)
    };
}

function validateSignedMatchResultPayload(ctx: nkruntime.Context, nk: nkruntime.Nakama, payload: SignedMatchResultPayload): { code: string; message: string } | null {
    if (!payload.matchKey || !payload.signature) {
        return { code: "invalid_payload", message: "Signed match result is missing required fields." };
    }

    if (!resolveMatchResultSecret(ctx)) {
        return { code: "backend_misconfigured", message: "Signed match result secret is not configured." };
    }

    const nowUnix = Math.floor(Date.now() / 1000);
    if (payload.issuedAtUnix <= 0 || Math.abs(nowUnix - payload.issuedAtUnix) > MATCH_RESULT_MAX_AGE_SECONDS) {
        return { code: "expired_signature", message: "Signed match result payload expired." };
    }

    const expectedSignature = computeSignedMatchResultSignature(ctx, nk, payload);
    if (expectedSignature !== payload.signature) {
        return { code: "invalid_signature", message: "Signed match result signature mismatch." };
    }

    return null;
}

function computeSignedMatchResultSignature(ctx: nkruntime.Context, nk: nkruntime.Nakama, payload: SignedMatchResultPayload): string {
    return arrayBufferToHex(nk.hmacSha256Hash(buildSignedMatchResultCanonicalString(payload), resolveMatchResultSecret(ctx)));
}

function buildSignedMatchResultCanonicalString(payload: SignedMatchResultPayload): string {
    return [
        clampMin(readNumber(payload.version, 1), 1),
        normalizeId(payload.matchKey),
        clampMin(readNumber(payload.issuedAtUnix, 0), 0),
        normalizeId(payload.mapId),
        normalizeId(payload.gameMode),
        normalizeId(payload.playerTeam),
        normalizeId(payload.winningTeam),
        payload.won ? 1 : 0,
        clampMin(readNumber(payload.attackerRoundsWon, 0), 0),
        clampMin(readNumber(payload.defenderRoundsWon, 0), 0),
        clampMin(readNumber(payload.kills, 0), 0),
        clampMin(readNumber(payload.deaths, 0), 0),
        clampMin(readNumber(payload.assists, 0), 0),
        payload.wasMvp ? 1 : 0,
        normalizeId(payload.heroId),
        clampMin(readNumber(payload.matchDurationSeconds, 0), 0),
        clampMin(readNumber(payload.headshotCount, 0), 0),
        clampMin(readNumber(payload.wallbangCount, 0), 0),
        clampMin(readNumber(payload.spherePlantsCount, 0), 0),
        clampMin(readNumber(payload.sphereDefusesCount, 0), 0),
        clampMin(readNumber(payload.ultimateActivations, 0), 0),
        clampMin(readNumber(payload.peakCreditsThisMatch, 0), 0),
        normalizeId(payload.mostUsedWeaponId)
    ].join("|");
}

function resolveMatchResultSecret(ctx: nkruntime.Context): string {
    return readStringEnv(ctx, MATCH_RESULT_SECRET_ENV, "");
}

function arrayBufferToHex(buffer: ArrayBuffer): string {
    const bytes = new Uint8Array(buffer);
    let out = "";
    bytes.forEach((byte) => {
        const hex = byte.toString(16);
        out += hex.length === 1 ? "0" + hex : hex;
    });
    return out;
}

function signedPayloadToRankedResult(payload: SignedMatchResultPayload, profile: PlayerProfileData): RankedResultPayload {
    const attackerRoundsWon = clampMin(readNumber(payload.attackerRoundsWon, 0), 0);
    const defenderRoundsWon = clampMin(readNumber(payload.defenderRoundsWon, 0), 0);
    const highScore = Math.max(attackerRoundsWon, defenderRoundsWon);
    const lowScore = Math.min(attackerRoundsWon, defenderRoundsWon);

    return {
        opponentAverageRating: clampMin(readNumber(profile.elo, MINIMUM_RATING), MINIMUM_RATING),
        won: !!payload.won,
        kills: clampMin(readNumber(payload.kills, 0), 0),
        deaths: clampMin(readNumber(payload.deaths, 0), 0),
        assists: clampMin(readNumber(payload.assists, 0), 0),
        roundsWon: payload.won ? highScore : lowScore,
        roundsLost: payload.won ? lowScore : highScore,
        wasMvp: !!payload.wasMvp
    };
}

function writeSignedTelemetry(userId: string, logger: nkruntime.Logger, nk: nkruntime.Nakama, payload: SignedMatchResultPayload, eloBefore: number, eloDelta: number): boolean {
    try {
        nk.storageWrite([{
            collection: TELEMETRY_COLLECTION,
            key: payload.matchKey,
            userId: userId,
            value: {
                user_id: userId,
                match_key: payload.matchKey,
                timestamp_unix: payload.issuedAtUnix,
                map_id: payload.mapId || "unknown_map",
                game_mode: payload.gameMode || "unknown_mode",
                winning_team: payload.winningTeam || "unknown_team",
                match_duration_sec: clampMin(readNumber(payload.matchDurationSeconds, 0), 0),
                total_rounds: clampMin(readNumber(payload.attackerRoundsWon, 0), 0) + clampMin(readNumber(payload.defenderRoundsWon, 0), 0),
                attacker_rounds_won: clampMin(readNumber(payload.attackerRoundsWon, 0), 0),
                defender_rounds_won: clampMin(readNumber(payload.defenderRoundsWon, 0), 0),
                kills: clampMin(readNumber(payload.kills, 0), 0),
                deaths: clampMin(readNumber(payload.deaths, 0), 0),
                assists: clampMin(readNumber(payload.assists, 0), 0),
                headshot_count: clampMin(readNumber(payload.headshotCount, 0), 0),
                wallbang_count: clampMin(readNumber(payload.wallbangCount, 0), 0),
                was_mvp: !!payload.wasMvp,
                hero_id: normalizeId(payload.heroId),
                most_used_weapon_id: normalizeId(payload.mostUsedWeaponId),
                sphere_plants: clampMin(readNumber(payload.spherePlantsCount, 0), 0),
                sphere_defuses: clampMin(readNumber(payload.sphereDefusesCount, 0), 0),
                ultimate_activations: clampMin(readNumber(payload.ultimateActivations, 0), 0),
                elo_before: clampMin(readNumber(eloBefore, MINIMUM_RATING), MINIMUM_RATING),
                elo_delta: readNumber(eloDelta, 0),
                peak_credits_this_match: clampMin(readNumber(payload.peakCreditsThisMatch, 0), 0)
            },
            permissionRead: 0,
            permissionWrite: 0
        }]);
        return true;
    } catch (error) {
        logger.info("Signed telemetry write skipped: " + errorToString(error));
        return false;
    }
}

function readMatchResultReceipt(userId: string, matchKey: string, nk: nkruntime.Nakama): MatchResultReceipt | null {
    const objects = nk.storageRead([{ collection: MATCH_RESULT_RECEIPTS_COLLECTION, key: matchKey, userId: userId }]);
    if (!objects || objects.length === 0) {
        return null;
    }

    return castMatchResultReceipt(objects[0].value);
}

function writeMatchResultReceipt(userId: string, matchKey: string, nk: nkruntime.Nakama, receipt: MatchResultReceipt): void {
    nk.storageWrite([{
        collection: MATCH_RESULT_RECEIPTS_COLLECTION,
        key: matchKey,
        userId: userId,
        value: receipt,
        permissionRead: 0,
        permissionWrite: 0
    }]);
}

function castMatchResultReceipt(raw: {[key: string]: any}): MatchResultReceipt {
    return {
        matchKey: normalizeId(raw.matchKey),
        previousRating: clampMin(readNumber(raw.previousRating, MINIMUM_RATING), MINIMUM_RATING),
        newRating: clampMin(readNumber(raw.newRating, MINIMUM_RATING), MINIMUM_RATING),
        delta: readNumber(raw.delta, 0),
        telemetrySaved: !!raw.telemetrySaved,
        processedAtUnix: clampMin(readNumber(raw.processedAtUnix, 0), 0)
    };
}

function resolveDefaultHero(profile: PlayerProfileData): string {
    for (const starterHeroId of STARTER_HERO_IDS) {
        if (containsString(profile.ownedHeroIds, starterHeroId)) {
            return starterHeroId;
        }
    }
    return profile.ownedHeroIds.length > 0 ? profile.ownedHeroIds[0] : STARTER_HERO_IDS[0];
}

function ensureWalletState(userId: string, profile: PlayerProfileData, nk: nkruntime.Nakama): WalletState {
    const account = nk.accountGetId(userId);
    const rawWallet = account?.wallet ?? {};

    const hasCommandCredits = hasWalletId(rawWallet, WALLET_ID_COMMAND_CREDITS);
    const hasZCore = hasWalletId(rawWallet, WALLET_ID_ZCORE);
    if (hasCommandCredits && hasZCore) {
        return walletStateFromRaw(rawWallet);
    }

    const bootstrapChanges: {[key: string]: number} = {};
    if (!hasCommandCredits) {
        const desiredCommandCredits = clampMin(readNumber(profile.commandCredits, STARTING_COMMAND_CREDITS), 0);
        if (desiredCommandCredits !== 0) {
            bootstrapChanges[WALLET_ID_COMMAND_CREDITS] = desiredCommandCredits;
        }
    }
    if (!hasZCore) {
        const desiredZCore = clampMin(readNumber(profile.zCore, STARTING_ZCORE), 0);
        if (desiredZCore !== 0) {
            bootstrapChanges[WALLET_ID_ZCORE] = desiredZCore;
        }
    }

    if (Object.keys(bootstrapChanges).length === 0) {
        return walletStateFromRaw(rawWallet);
    }

    const walletResult = nk.walletUpdate(userId, bootstrapChanges, { reason: "wallet_bootstrap" }, false);
    return walletStateFromRaw(walletResult.updated);
}

function readWalletState(userId: string, nk: nkruntime.Nakama): WalletState {
    const account = nk.accountGetId(userId);
    return walletStateFromRaw(account?.wallet ?? {});
}

function applyWalletStateToProfile(profile: PlayerProfileData, wallet: WalletState): void {
    profile.commandCredits = wallet.commandCredits;
    profile.currency = wallet.commandCredits;
    profile.zCore = wallet.zCore;
}

function walletStateFromRaw(wallet: {[key: string]: number}): WalletState {
    return {
        commandCredits: clampMin(readNumber(wallet[WALLET_ID_COMMAND_CREDITS], 0), 0),
        zCore: clampMin(readNumber(wallet[WALLET_ID_ZCORE], 0), 0)
    };
}

function hasWalletId(wallet: {[key: string]: number}, key: string): boolean {
    return !!wallet && typeof wallet[key] === "number";
}

function getWalletId(currencyType: number): string | null {
    if (currencyType === CURRENCY_COMMAND_CREDITS) return WALLET_ID_COMMAND_CREDITS;
    if (currencyType === CURRENCY_ZCORE) return WALLET_ID_ZCORE;
    return null;
}

function buildRankedResultPayload(raw: any, profile: PlayerProfileData): RankedResultPayload {
    return {
        opponentAverageRating: clampMin(readNumber(raw.opponentAverageRating, profile.elo), MINIMUM_RATING),
        won: !!raw.won,
        kills: clampMin(readNumber(raw.kills, 0), 0),
        deaths: clampMin(readNumber(raw.deaths, 0), 0),
        assists: clampMin(readNumber(raw.assists, 0), 0),
        roundsWon: clampMin(readNumber(raw.roundsWon, 0), 0),
        roundsLost: clampMin(readNumber(raw.roundsLost, 0), 0),
        wasMvp: !!raw.wasMvp
    };
}

function calculateRankedRatingDelta(performance: RankedResultPayload, playerRating: number, rankedMatchesPlayed: number): number {
    const safePlayerRating = clampMin(readNumber(playerRating, MINIMUM_RATING), MINIMUM_RATING);
    const safeOpponentRating = performance.opponentAverageRating > 0
        ? clampMin(performance.opponentAverageRating, MINIMUM_RATING)
        : safePlayerRating;

    const expectedScore = 1 / (1 + Math.pow(10, (safeOpponentRating - safePlayerRating) / 400));
    const actualScore = performance.won ? 1 : 0;
    const kFactor = getRankedKFactor(safePlayerRating, rankedMatchesPlayed);
    const baseDelta = Math.round(kFactor * (actualScore - expectedScore));
    const performanceBonus = calculatePerformanceBonus(performance);
    const delta = baseDelta + performanceBonus;

    if (performance.won) {
        const maxGain = rankedMatchesPlayed < PLACEMENT_MATCHES ? 48 : 38;
        return clamp(delta, 10, maxGain);
    }

    const maxLoss = rankedMatchesPlayed < PLACEMENT_MATCHES ? -34 : -28;
    return clamp(delta, maxLoss, 0);
}

function applyRankedRatingDelta(currentRating: number, delta: number): number {
    return clampMin(readNumber(currentRating, MINIMUM_RATING) + readNumber(delta, 0), MINIMUM_RATING);
}

function getRankedKFactor(rating: number, rankedMatchesPlayed: number): number {
    if (rankedMatchesPlayed < PLACEMENT_MATCHES) return 40;
    if (rating < 1600) return 32;
    if (rating < 2200) return 28;
    if (rating < 2800) return 24;
    if (rating < 3400) return 20;
    return 16;
}

function calculatePerformanceBonus(performance: RankedResultPayload): number {
    const roundsPlayed = Math.max(1, performance.roundsWon + performance.roundsLost);
    const contribution = performance.kills + (performance.assists * 0.65);
    const expectedContribution = Math.max(3, roundsPlayed * 0.45);
    const contributionOffset = clamp(
        (contribution - expectedContribution) / Math.max(4, roundsPlayed * 0.35),
        -1,
        1);

    const survivalScore = performance.deaths <= 0
        ? 1
        : clamp((performance.kills + (performance.assists * 0.5)) / performance.deaths, 0, 2);

    const roundMomentum = clamp((performance.roundsWon - performance.roundsLost) / roundsPlayed, -1, 1);
    const rawBonus = (contributionOffset * 4)
        + ((survivalScore - 0.5) * 4)
        + (roundMomentum * 3)
        + (performance.wasMvp ? 2 : 0);

    let bonus = Math.round(rawBonus);
    if (performance.won) {
        // Keep positive bonus for wins as-is.
    } else {
        bonus = Math.min(bonus, 2);
    }

    return clamp(bonus, -6, 8);
}

function ownsHero(profile: PlayerProfileData, heroId: string): boolean { return containsString(profile.ownedHeroIds, heroId); }
function isKnownHero(heroId: string): boolean { return containsString(ALL_HERO_IDS, heroId); }
function getHeroDisplayName(heroId: string): string { return HERO_DISPLAY_NAMES[heroId] || heroId || "Unknown Hero"; }
function parsePayload(payload: string): any { return payload ? JSON.parse(payload) : {}; }
function normalizeId(value: any): string { return typeof value === "string" ? value.trim().toLowerCase() : ""; }
function stringifyValue(value: any, fallback: string): string { return typeof value === "string" && value.length > 0 ? value : fallback; }
function readNumber(value: any, fallback: number): number {
    const parsed = Number(value);
    if (!isFinite(parsed)) {
        return fallback;
    }

    return parsed;
}
function clampMin(value: number, minimum: number): number { return Math.max(value, minimum); }
function clamp(value: number, minimum: number, maximum: number): number { return Math.min(Math.max(value, minimum), maximum); }
function errorToString(error: unknown): string { return error instanceof Error ? error.message : String(error); }
function containsString(source: string[], value: string): boolean {
    for (const sourceValue of source) {
        if (sourceValue === value) {
            return true;
        }
    }
    return false;
}
function normalizeIds(values: any): string[] {
    const src = Array.isArray(values) ? values : [];
    const out: string[] = [];
    for (const value of src) {
        const id = normalizeId(value);
        if (id && !containsString(out, id)) {
            out.push(id);
        }
    }
    return out;
}
function readStringEnv(ctx: nkruntime.Context, key: string, fallback: string): string {
    const value = ctx.env?.[key];
    return value || fallback;
}
function readNumberEnv(ctx: nkruntime.Context, key: string, fallback: number): number { return readNumber(readStringEnv(ctx, key, String(fallback)), fallback); }
function readBoolEnv(ctx: nkruntime.Context, key: string, fallback: boolean): boolean {
    const value = readStringEnv(ctx, key, fallback ? "true" : "false").toLowerCase();
    return value === "1" || value === "true" || value === "yes" || value === "on";
}
