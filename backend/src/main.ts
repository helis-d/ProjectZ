const STORAGE_COLLECTION = "player_data";
const STORAGE_KEY_PROFILE = "profile";

const RPC_BACKEND_HEALTH = "projectz_backend_health";
const RPC_SELECT_HERO = "projectz_select_hero";
const RPC_UNLOCK_HERO = "projectz_unlock_hero";
const RPC_PURCHASE_OFFER = "projectz_purchase_offer";
const RPC_APPLY_RANKED_RESULT = "projectz_apply_ranked_result";
const RPC_SUBMIT_MATCH_TELEMETRY = "projectz_submit_match_telemetry";

const MATCH_HANDLER_ID = "custom_lobby";
const RANKED_LEADERBOARD_ID = "ranked_rating";
const TELEMETRY_COLLECTION = "match_telemetry";

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
    initializer.registerRpc(RPC_SELECT_HERO, RpcSelectHero);
    initializer.registerRpc(RPC_UNLOCK_HERO, RpcUnlockHero);
    initializer.registerRpc(RPC_PURCHASE_OFFER, RpcPurchaseOffer);
    initializer.registerRpc(RPC_APPLY_RANKED_RESULT, RpcApplyRankedResult);
    initializer.registerRpc(RPC_SUBMIT_MATCH_TELEMETRY, RpcSubmitMatchTelemetry);
};

function MatchmakerMatched(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, matches: nkruntime.MatchmakerResult[]): string {
    var userIds = matches.map(function(match) { return match.presence.userId; });
    return nk.matchCreate(MATCH_HANDLER_ID, {
        ip: readStringEnv(ctx, MATCH_SERVER_ADDRESS_ENV, "127.0.0.1"),
        port: readNumberEnv(ctx, MATCH_SERVER_PORT_ENV, 7770),
        allowedUserIds: userIds
    });
}

function MatchInit(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, params: {[key: string]: any}) {
    var state: LobbyMatchState = {
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
    return JSON.stringify({ succeeded: true, message: "ProjectZ backend online.", backendVersion: "phase2", rankedLeaderboardId: RANKED_LEADERBOARD_ID });
}

function RpcSelectHero(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, payload: string): string {
    return runProfileRpc(ctx, logger, nk, function(profile) {
        var request = parsePayload(payload);
        var heroId = normalizeId(request.heroId);
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
    return runProfileRpc(ctx, logger, nk, function(profile) {
        var request = parsePayload(payload);
        var purchase = tryUnlockHero(profile, normalizeId(request.heroId));
        return response(purchase.status === PURCHASE_STATUS_SUCCESS, purchaseStatusToCode(purchase.status), purchase.message, profile, purchase, purchase.status === PURCHASE_STATUS_SUCCESS);
    });
}

function RpcPurchaseOffer(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, payload: string): string {
    return runProfileRpc(ctx, logger, nk, function(profile) {
        var request = parsePayload(payload);
        var purchase = tryPurchaseOffer(ctx, profile, getOfferById(normalizeId(request.offerId)));
        return response(purchase.status === PURCHASE_STATUS_SUCCESS, purchaseStatusToCode(purchase.status), purchase.message, profile, purchase, purchase.status === PURCHASE_STATUS_SUCCESS);
    });
}

function RpcApplyRankedResult(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, payload: string): string {
    return runProfileRpc(ctx, logger, nk, function(profile) {
        var request = parsePayload(payload);
        var performance = buildRankedResultPayload(request, profile);
        var previousRating = profile.elo;
        var delta = calculateRankedRatingDelta(performance, previousRating, profile.rankedMatchesPlayed);
        var newRating = applyRankedRatingDelta(previousRating, delta);

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
                ctx.userId!,
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

        return rankedResponse(true, null, "Ranked result persisted.", profile, previousRating, newRating, delta, true);
    });
}

function RpcSubmitMatchTelemetry(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, payload: string): string {
    if (!ctx.userId) {
        return JSON.stringify(telemetryResponse(false, "unauthorized", "Authentication required.", null));
    }

    try {
        var request = parsePayload(payload);
        var matchKey = nk.uuidv4().replace(/-/g, "");
        var timestampUnix = Math.floor(Date.now() / 1000);
        var telemetry = {
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

function runProfileRpc(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, handler: (profile: PlayerProfileData) => any): string {
    if (!ctx.userId) {
        return JSON.stringify(response(false, "unauthorized", "Authentication required.", null));
    }
    try {
        var loaded = loadOrCreateProfile(ctx, nk);
        var result = handler(loaded.profile);
        if (result.persist) {
            writeProfile(ctx.userId, loaded.profile, loaded.version, nk);
        }
        delete result.persist;
        return JSON.stringify(result);
    } catch (error) {
        logger.error("Profile RPC failed: " + errorToString(error));
        return JSON.stringify(response(false, "server_error", "Backend operation failed.", null));
    }
}

function loadOrCreateProfile(ctx: nkruntime.Context, nk: nkruntime.Nakama) {
    var objects = nk.storageRead([{ collection: STORAGE_COLLECTION, key: STORAGE_KEY_PROFILE, userId: ctx.userId! }]);
    if (objects && objects.length > 0) {
        return { profile: sanitizeProfile(castProfile(objects[0].value)), version: objects[0].version };
    }
    var profile = createDefaultProfile(ctx.username || "NewPlayer");
    writeProfile(ctx.userId!, profile, null, nk);
    return { profile: profile, version: null };
}

function writeProfile(userId: string, profile: PlayerProfileData, version: string | null, nk: nkruntime.Nakama): void {
    var write: nkruntime.StorageWriteRequest = {
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
    for (var i = 0; i < STARTER_HERO_IDS.length; i++) {
        if (!containsString(profile.ownedHeroIds, STARTER_HERO_IDS[i])) {
            profile.ownedHeroIds.push(STARTER_HERO_IDS[i]);
        }
    }
    profile.selectedHero = ownsHero(profile, normalizeId(profile.selectedHero)) ? normalizeId(profile.selectedHero) : resolveDefaultHero(profile);
    return profile;
}

function tryUnlockHero(profile: PlayerProfileData, heroId: string) {
    if (!isKnownHero(heroId)) {
        return purchase(PURCHASE_STATUS_UNKNOWN_HERO, "hero_unlock_" + heroId, heroId, "Unknown hero unlock request.", CURRENCY_NONE, 0);
    }
    if (ownsHero(profile, heroId)) {
        return purchase(PURCHASE_STATUS_ALREADY_OWNED, "hero_unlock_" + heroId, heroId, "Hero already owned.", CURRENCY_NONE, 0);
    }
    if (!spendCurrency(profile, CURRENCY_COMMAND_CREDITS, DEFAULT_HERO_UNLOCK_PRICE)) {
        return purchase(PURCHASE_STATUS_INSUFFICIENT_FUNDS, "hero_unlock_" + heroId, heroId, "Not enough Command Credits.", CURRENCY_COMMAND_CREDITS, 0);
    }
    profile.ownedHeroIds.push(heroId);
    profile.ownedHeroIds = normalizeIds(profile.ownedHeroIds);
    return purchase(PURCHASE_STATUS_SUCCESS, "hero_unlock_" + heroId, heroId, getHeroDisplayName(heroId) + " unlocked permanently.", CURRENCY_COMMAND_CREDITS, DEFAULT_HERO_UNLOCK_PRICE);
}

function tryPurchaseOffer(ctx: nkruntime.Context, profile: PlayerProfileData, offer: CatalogOffer | null) {
    if (!offer) {
        return purchase(PURCHASE_STATUS_INVALID_OFFER, null, null, "Offer not found.", CURRENCY_NONE, 0);
    }
    if (!isOfferActive(ctx, offer)) {
        return purchase(PURCHASE_STATUS_OFFER_NOT_ACTIVE, offer.offerId, offer.contentId, "Offer is not currently active.", CURRENCY_NONE, 0);
    }
    if (offer.offerType === OFFER_TYPE_HERO_UNLOCK) {
        return tryUnlockHero(profile, normalizeId(offer.contentId));
    }
    if (ownsOffer(profile, offer)) {
        return purchase(PURCHASE_STATUS_ALREADY_OWNED, offer.offerId, offer.contentId, "Offer already owned.", CURRENCY_NONE, 0);
    }
    if (!spendCurrency(profile, offer.priceCurrency, offer.price)) {
        return purchase(PURCHASE_STATUS_INSUFFICIENT_FUNDS, offer.offerId, offer.contentId, "Insufficient funds.", offer.priceCurrency, 0);
    }
    grantOffer(profile, offer);
    return purchase(PURCHASE_STATUS_SUCCESS, offer.offerId, offer.contentId, offer.displayName + " purchased successfully.", offer.priceCurrency, offer.price);
}

function getOfferById(offerId: string): CatalogOffer | null {
    var offers = getCatalogOffers();
    for (var i = 0; i < offers.length; i++) {
        if (normalizeId(offers[i].offerId) === offerId) {
            return offers[i];
        }
    }
    return null;
}

function getCatalogOffers(): CatalogOffer[] {
    var offers: CatalogOffer[] = [];
    for (var i = 0; i < ALL_HERO_IDS.length; i++) {
        if (!containsString(STARTER_HERO_IDS, ALL_HERO_IDS[i])) {
            offers.push({ offerId: "hero_unlock_" + ALL_HERO_IDS[i], displayName: getHeroDisplayName(ALL_HERO_IDS[i]) + " Hero Unlock", offerType: OFFER_TYPE_HERO_UNLOCK, priceCurrency: CURRENCY_COMMAND_CREDITS, price: DEFAULT_HERO_UNLOCK_PRICE, contentId: ALL_HERO_IDS[i], availability: AVAILABILITY_LAUNCH });
        }
    }
    offers.push({ offerId: "launch_weaponskin_vandal_firstlight", displayName: "Firstlight Vandal", offerType: OFFER_TYPE_WEAPON_SKIN, priceCurrency: CURRENCY_ZCORE, price: 900, contentId: "weaponskin_vandal_firstlight", availability: AVAILABILITY_LAUNCH });
    offers.push({ offerId: "launch_playercard_founders_signal", displayName: "Founder's Signal Card", offerType: OFFER_TYPE_PLAYER_CARD, priceCurrency: CURRENCY_ZCORE, price: 250, contentId: "playercard_founders_signal", availability: AVAILABILITY_LAUNCH });
    offers.push({ offerId: "launch_spray_hold_the_site", displayName: "Hold The Site Spray", offerType: OFFER_TYPE_SPRAY, priceCurrency: CURRENCY_ZCORE, price: 175, contentId: "spray_hold_the_site", availability: AVAILABILITY_LAUNCH });
    offers.push({ offerId: "launch_charm_quantum_key", displayName: "Quantum Key Charm", offerType: OFFER_TYPE_CHARM, priceCurrency: CURRENCY_ZCORE, price: 225, contentId: "charm_quantum_key", availability: AVAILABILITY_LAUNCH });
    offers.push({ offerId: "alpha_founder_pack", displayName: "Alpha Founder Pack", offerType: OFFER_TYPE_ALPHA_FOUNDER_PACK, priceCurrency: CURRENCY_ZCORE, price: 1800, contentId: "alpha_founder_pack", availability: AVAILABILITY_ALPHA_ONLY });
    offers.push({ offerId: "season2_battlepass_premium", displayName: "Season 2 Battle Pass", offerType: OFFER_TYPE_BATTLE_PASS, priceCurrency: CURRENCY_ZCORE, price: 1000, contentId: "season2_battlepass_premium", availability: AVAILABILITY_SEASON2 });
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

function spendCurrency(profile: PlayerProfileData, currencyType: number, amount: number): boolean {
    if (currencyType === CURRENCY_COMMAND_CREDITS) {
        if (profile.commandCredits < amount) return false;
        profile.commandCredits -= amount;
        profile.currency = profile.commandCredits;
        return true;
    }
    if (currencyType === CURRENCY_ZCORE) {
        if (profile.zCore < amount) return false;
        profile.zCore -= amount;
        return true;
    }
    return false;
}

function response(succeeded: boolean, errorCode: string | null, message: string, profile: PlayerProfileData | null, purchaseResult?: any, persist?: boolean) {
    return { succeeded: succeeded, errorCode: errorCode, message: message, profile: profile, purchase: purchaseResult || null, persist: !!persist };
}

function rankedResponse(succeeded: boolean, errorCode: string | null, message: string, profile: PlayerProfileData | null, previousRating: number, newRating: number, delta: number, persist: boolean) {
    return {
        succeeded: succeeded,
        errorCode: errorCode,
        message: message,
        profile: profile,
        previousRating: previousRating,
        newRating: newRating,
        delta: delta,
        persist: persist
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

function resolveDefaultHero(profile: PlayerProfileData): string {
    for (var i = 0; i < STARTER_HERO_IDS.length; i++) if (containsString(profile.ownedHeroIds, STARTER_HERO_IDS[i])) return STARTER_HERO_IDS[i];
    return profile.ownedHeroIds.length > 0 ? profile.ownedHeroIds[0] : STARTER_HERO_IDS[0];
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
    var safePlayerRating = clampMin(readNumber(playerRating, MINIMUM_RATING), MINIMUM_RATING);
    var safeOpponentRating = performance.opponentAverageRating > 0
        ? clampMin(performance.opponentAverageRating, MINIMUM_RATING)
        : safePlayerRating;

    var expectedScore = 1 / (1 + Math.pow(10, (safeOpponentRating - safePlayerRating) / 400));
    var actualScore = performance.won ? 1 : 0;
    var kFactor = getRankedKFactor(safePlayerRating, rankedMatchesPlayed);
    var baseDelta = Math.round(kFactor * (actualScore - expectedScore));
    var performanceBonus = calculatePerformanceBonus(performance);
    var delta = baseDelta + performanceBonus;

    if (performance.won) {
        var maxGain = rankedMatchesPlayed < PLACEMENT_MATCHES ? 48 : 38;
        return clamp(delta, 10, maxGain);
    }

    var maxLoss = rankedMatchesPlayed < PLACEMENT_MATCHES ? -34 : -28;
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
    var roundsPlayed = Math.max(1, performance.roundsWon + performance.roundsLost);
    var contribution = performance.kills + (performance.assists * 0.65);
    var expectedContribution = Math.max(3, roundsPlayed * 0.45);
    var contributionOffset = clamp(
        (contribution - expectedContribution) / Math.max(4, roundsPlayed * 0.35),
        -1,
        1);

    var survivalScore = performance.deaths <= 0
        ? 1
        : clamp((performance.kills + (performance.assists * 0.5)) / performance.deaths, 0, 2);

    var roundMomentum = clamp((performance.roundsWon - performance.roundsLost) / roundsPlayed, -1, 1);
    var rawBonus = (contributionOffset * 4)
        + ((survivalScore - 0.5) * 4)
        + (roundMomentum * 3)
        + (performance.wasMvp ? 2 : 0);

    var bonus = Math.round(rawBonus);
    if (!performance.won) {
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
function readNumber(value: any, fallback: number): number { var parsed = Number(value); return isNaN(parsed) ? fallback : parsed; }
function clampMin(value: number, minimum: number): number { return value < minimum ? minimum : value; }
function clamp(value: number, minimum: number, maximum: number): number { return value < minimum ? minimum : (value > maximum ? maximum : value); }
function errorToString(error: unknown): string { return error instanceof Error ? error.message : String(error); }
function containsString(source: string[], value: string): boolean { for (var i = 0; i < source.length; i++) if (source[i] === value) return true; return false; }
function normalizeIds(values: any): string[] { var src = Array.isArray(values) ? values : []; var out: string[] = []; for (var i = 0; i < src.length; i++) { var id = normalizeId(src[i]); if (id && !containsString(out, id)) out.push(id); } return out; }
function readStringEnv(ctx: nkruntime.Context, key: string, fallback: string): string { return ctx.env && ctx.env[key] ? ctx.env[key] : fallback; }
function readNumberEnv(ctx: nkruntime.Context, key: string, fallback: number): number { return readNumber(readStringEnv(ctx, key, String(fallback)), fallback); }
function readBoolEnv(ctx: nkruntime.Context, key: string, fallback: boolean): boolean { var value = readStringEnv(ctx, key, fallback ? "true" : "false").toLowerCase(); return value === "1" || value === "true" || value === "yes" || value === "on"; }
