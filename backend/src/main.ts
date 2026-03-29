const rpcIdMatchmakerMatched = "matchmaker_matched";

let InitModule: nkruntime.InitModule = function(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, initializer: nkruntime.Initializer) {
    logger.info("Initializing Nakama ProjectZ matchmaker module.");

    // This hook is called automatically when Nakama's Matchmaker finds a match
    initializer.registerMatchmakerMatched(matchmakerMatched);
};

// Orchestrator Payload
interface AllocatorPayload {
    match_id: string;
    players: string[]; // User IDs
}

// Function called when a match is successfully found
function matchmakerMatched(context: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, matches: nkruntime.MatchmakerResult[]): string {
    logger.info("Matchmaker matched! Creating dedicated server allocation...");

    const users = matches.map(m => m.presence.userId);

    // Normally here you would call your orchestrator (like Agones, Edgegap, or a custom EC2 manager)
    // using nk.httpRequest() to request a new Headless Unity container.
    // 
    // Example:
    /*
    const headers = { "Content-Type": "application/json", "Authorization": "Bearer YOUR_EDGEGAP_TOKEN" };
    const body = JSON.stringify({ match_id: context.matchId, players: users });
    const response = nk.httpRequest("https://api.edgegap.com/v1/deploy", "POST", headers, body);
    
    // Parse response to get IP and Port
    const deployResponse = JSON.parse(response.body);
    const serverIp = deployResponse.public_ip;
    const serverPort = deployResponse.ports["7770"].external;
    */

    // For now, since we don't have a real orchestrator connected, we simulate an IP/Port.
    // In production, DO NOT return a match ID here if the HTTP request fails.
    
    const serverIp = "127.0.0.1"; // Replace with orchestrator IP
    const serverPort = 7770;      // Replace with orchestrator Port

    // Create a custom match that simply relays the IP and Port back to the clients
    // using Nakama's authoritative match system. Alternatively, since this hook expects a Match ID string:
    // We can return a specific string format that the Unity client parses, 
    // OR we can create an authoritative match that broadcasts it.
    
    // We'll create an authoritative match ticket. 
    // In Unity, when IMatchmakerMatched fires, if we return a created custom match ID:
    const matchId = nk.matchCreate("custom_lobby", { ip: serverIp, port: serverPort });
    logger.info(`Created lobby match ${matchId} for routing players.`);

    return matchId;
}

// The custom match state that holds the IP/Port and tells connecting clients
interface LobbyMatchState {
    ip: string;
    port: number;
}

// Custom match handlers for "custom_lobby"
let matchInit: nkruntime.MatchInitFunction = function(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, params: {[key: string]: string}): {state: nkruntime.MatchState, tickRate: number, label: string} {
    const state: LobbyMatchState = {
        ip: params["ip"] as string,
        port: parseInt(params["port"] as string)
    };
    return {
        state,
        tickRate: 1, // 1 tick per second is enough just to sit here
        label: ""
    };
};

let matchJoinAttempt: nkruntime.MatchJoinAttemptFunction = function(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, dispatcher: nkruntime.MatchDispatcher, tick: number, state: nkruntime.MatchState, presence: nkruntime.Presence, metadata: {[key: string]: any}): {state: nkruntime.MatchState, accept: boolean, rejectMessage?: string} | null {
    return { state, accept: true };
};

let matchJoin: nkruntime.MatchJoinFunction = function(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, dispatcher: nkruntime.MatchDispatcher, tick: number, state: nkruntime.MatchState, presences: nkruntime.Presence[]): {state: nkruntime.MatchState} | null {
    // When a player joins this temporary Nakama match, we broadcast the Dedicated Server IP/Port via match data
    const s = state as LobbyMatchState;
    const payload = JSON.stringify({ ip: s.ip, port: s.port });
    
    // OpCode 1 = Dedicated Server Info
    dispatcher.broadcastMessage(1, payload, presences, null, true);
    
    return { state: s };
};

let matchLeave: nkruntime.MatchLeaveFunction = function(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, dispatcher: nkruntime.MatchDispatcher, tick: number, state: nkruntime.MatchState, presences: nkruntime.Presence[]): {state: nkruntime.MatchState} | null {
    return { state };
};

let matchLoop: nkruntime.MatchLoopFunction = function(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, dispatcher: nkruntime.MatchDispatcher, tick: number, state: nkruntime.MatchState, messages: nkruntime.MatchMessage[]): {state: nkruntime.MatchState} | null {
    // Just a basic loop
    return { state };
};

let matchTerminate: nkruntime.MatchTerminateFunction = function(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, dispatcher: nkruntime.MatchDispatcher, tick: number, state: nkruntime.MatchState, graceSeconds: number): {state: nkruntime.MatchState} | null {
    return { state };
};
