import { useState, useEffect, useRef } from 'react'
import * as signalR from '@microsoft/signalr'
import { MessagePackHubProtocol } from '@microsoft/signalr-protocol-msgpack'
import { motion, AnimatePresence } from 'framer-motion'
import { 
  Server, 
  Users, 
  Play, 
  RefreshCw, 
  Plus, 
  Wifi, 
  WifiOff, 
  Terminal, 
  Shield, 
  Sword,
  Search,
  Activity,
  Globe,
  Lock,
  ArrowRight
} from 'lucide-react'

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { cn } from "@/lib/utils"

// Types from server
interface GameInfoDto {
  gameId: string;
  playerCount: number;
  isStarted: boolean;
}

// Dynamic server URL resolution
const getHubUrl = () => {
  const hostname = window.location.hostname;
  const protocol = window.location.protocol === 'https:' ? 'https:' : 'http:';
  // If we're on a custom domain, assume server is on the same host but port 5128
  return `${protocol}//${hostname}:5128/gameHub`;
};

export default function App() {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [games, setGames] = useState<GameInfoDto[]>([]);
  const [status, setStatus] = useState<'disconnected' | 'connecting' | 'connected' | 'error'>('disconnected');
  const [error, setError] = useState<string | null>(null);
  const [newGameId, setNewGameId] = useState('');
  const [isInGame, setIsInGame] = useState(false);
  const [currentGameId, setCurrentGameId] = useState<string | null>(null);
  const [logs, setLogs] = useState<string[]>([]);

  const addLog = (msg: string) => {
    setLogs(prev => [msg, ...prev].slice(0, 50));
  };

  const connect = async () => {
    const hubUrl = getHubUrl();
    setStatus('connecting');
    setError(null);
    addLog(`Initiating handshake with ${hubUrl}...`);

    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withHubProtocol(new MessagePackHubProtocol())
      .withAutomaticReconnect()
      .build();

    newConnection.on("ReceiveGameList", (gameList: GameInfoDto[]) => {
      setGames(gameList);
      addLog(`Synchronized ${gameList.length} active sectors.`);
    });

    newConnection.on("ReceiveChat", (sender: string, message: string) => {
      addLog(`[COMM] ${sender}: ${message}`);
    });

    try {
      await newConnection.start();
      setConnection(newConnection);
      setStatus('connected');
      addLog("Authentication successful. Uplink established.");
      
      // Get initial games
      await newConnection.invoke("GetActiveGames");
    } catch (err: any) {
      console.error(err);
      const msg = err.message || "Uplink failure.";
      setError(msg);
      setStatus('error');
      addLog(`CRITICAL: ${msg}`);
    }
  };

  const refreshGames = async () => {
    if (connection) {
      addLog("Refreshing sector scans...");
      await connection.invoke("GetActiveGames");
    }
  };

  const createGame = async () => {
    if (!connection || !newGameId) return;
    try {
      addLog(`Deploying mission: ${newGameId}...`);
      await connection.invoke("JoinGame", newGameId);
      setCurrentGameId(newGameId);
      setIsInGame(true);
      refreshGames();
    } catch (err: any) {
      addLog(`DEPLOYMENT FAILED: ${err.message}`);
    }
  };

  const joinGame = async (gameId: string) => {
    if (!connection) return;
    try {
      addLog(`Joining sector: ${gameId}...`);
      await connection.invoke("JoinGame", gameId);
      setCurrentGameId(gameId);
      setIsInGame(true);
      refreshGames();
    } catch (err: any) {
      addLog(`ENTRY DENIED: ${err.message}`);
    }
  };

  return (
    <div className="min-h-screen bg-background text-foreground selection:bg-primary selection:text-background pb-20">
      {/* Top Banner */}
      <div className="h-1 bg-gradient-to-r from-transparent via-primary to-transparent opacity-50" />
      
      <div className="max-w-6xl mx-auto px-6 pt-12">
        {/* Navigation / Header */}
        <header className="flex flex-col md:flex-row justify-between items-start md:items-center gap-6 mb-16">
          <div className="flex items-center gap-6 group">
            <div className="relative">
              <div className="absolute -inset-2 bg-primary/20 blur-xl group-hover:bg-primary/40 transition-all rounded-full" />
              <div className="relative w-14 h-14 glass-panel flex items-center justify-center rounded-xl border-primary/50">
                <Shield className="text-primary animate-pulse" size={32} />
              </div>
            </div>
            <div>
              <h1 className="text-4xl font-black tracking-tighter uppercase italic leading-none">
                Tower<span className="text-primary">Fluffy</span>
              </h1>
              <div className="flex items-center gap-2 mt-1">
                <Badge variant="outline" className="text-[10px] border-primary/20 text-primary/70">PROD_ENV</Badge>
                <span className="text-[10px] text-white/30 uppercase font-bold tracking-widest">Protocol v1.0.4</span>
              </div>
            </div>
          </div>

          <div className="flex items-center gap-4">
            <div className={cn(
              "flex items-center gap-3 px-6 py-2.5 rounded-full border transition-all",
              status === 'connected' ? "border-primary/30 bg-primary/5 neon-glow-primary" : "border-white/10 bg-white/5"
            )}>
              <div className={cn("w-2 h-2 rounded-full", status === 'connected' ? "bg-primary animate-ping" : "bg-white/20")} />
              <span className={cn(
                "text-xs font-black uppercase tracking-widest",
                status === 'connected' ? "text-primary" : "text-white/40"
              )}>
                {status}
              </span>
            </div>
            {status !== 'connected' && (
              <Button onClick={connect} variant="neon" size="lg">Initialize Link</Button>
            )}
          </div>
        </header>

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-10">
          {/* Controls Column */}
          <div className="lg:col-span-4 space-y-8">
            <Card className="neon-glow-secondary border-secondary/30">
              <CardHeader>
                <CardTitle className="text-secondary text-lg flex items-center gap-2">
                  <Plus size={20} /> Deploy Mission
                </CardTitle>
                <CardDescription>Target a specific game sector</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="relative">
                  <Terminal className="absolute left-3 top-3 text-white/20" size={16} />
                  <Input 
                    placeholder="MISSION_CODENAME" 
                    value={newGameId}
                    onChange={(e) => setNewGameId(e.target.value)}
                    className="pl-10"
                  />
                </div>
                <Button 
                  onClick={createGame} 
                  disabled={!connection || !newGameId}
                  variant="neon-pink" 
                  className="w-full h-12"
                >
                  Confirm Deployment
                </Button>
              </CardContent>
            </Card>

            <Card className="bg-black/60 border-white/5">
              <CardHeader className="pb-2">
                <CardTitle className="text-white/40 text-[10px] tracking-[0.3em] flex items-center gap-2">
                  <Activity size={12} /> System Telemetry
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="h-64 overflow-y-auto space-y-1.5 mono text-[10px] text-white/40 scrollbar-hide">
                  {logs.length === 0 && <div className="italic">Awaiting uplink...</div>}
                  {logs.map((log, i) => (
                    <div key={i} className={cn(
                      "border-l-2 pl-3 py-1",
                      log.includes("CRITICAL") ? "border-red-500 text-red-400 bg-red-500/5" : 
                      log.includes("successful") ? "border-primary text-primary/80 bg-primary/5" :
                      "border-white/10"
                    )}>
                      <span className="opacity-30 mr-2">[{new Date().toLocaleTimeString([], {hour12: false})}]</span>
                      {log}
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Lobby Column */}
          <div className="lg:col-span-8">
            <div className="flex justify-between items-end mb-8 px-2">
              <div>
                <h2 className="text-2xl font-bold flex items-center gap-3">
                  <Globe className="text-primary" /> Active Sectors
                </h2>
                <p className="text-sm text-white/40 uppercase tracking-widest mt-1">Found {games.length} operational zones</p>
              </div>
              <Button onClick={refreshGames} variant="ghost" size="icon" className="rounded-full hover:text-primary">
                <RefreshCw size={20} className={cn(status === 'connecting' && "animate-spin")} />
              </Button>
            </div>

            <div className="grid grid-cols-1 gap-4">
              <AnimatePresence mode="popLayout">
                {games.length === 0 ? (
                  <motion.div 
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                    className="glass-panel rounded-2xl p-20 flex flex-col items-center justify-center text-center border-dashed border-white/5"
                  >
                    <div className="w-20 h-20 bg-white/5 rounded-full flex items-center justify-center mb-6">
                      <Search size={32} className="text-white/20" />
                    </div>
                    <h3 className="text-lg font-bold uppercase mb-2">No signals detected</h3>
                    <p className="text-sm text-white/30 max-w-xs mx-auto">
                      All sectors are currently offline or encrypted. Initialize a new link to begin deployment.
                    </p>
                  </motion.div>
                ) : (
                  games.map((game) => (
                    <motion.div
                      key={game.gameId}
                      initial={{ opacity: 0, x: 20 }}
                      animate={{ opacity: 1, x: 0 }}
                      exit={{ opacity: 0, scale: 0.95 }}
                      layout
                    >
                      <Card className="hover:bg-primary/5 transition-all cursor-default">
                        <CardContent className="p-0">
                          <div className="flex flex-col sm:flex-row items-center p-4 sm:p-6 gap-6">
                            <div className="w-16 h-16 rounded-xl bg-primary/10 flex items-center justify-center border border-primary/20 shrink-0">
                              <Play className="text-primary fill-primary/20" size={24} />
                            </div>
                            
                            <div className="flex-1 text-center sm:text-left">
                              <div className="flex flex-col sm:flex-row sm:items-center gap-2 mb-2">
                                <h4 className="text-xl font-black mono tracking-tighter uppercase">{game.gameId}</h4>
                                <Badge variant={game.isStarted ? "destructive" : "neon"} className="w-fit mx-auto sm:mx-0">
                                  {game.isStarted ? "In Combat" : "Recruiting"}
                                </Badge>
                              </div>
                              <div className="flex items-center justify-center sm:justify-start gap-4 text-xs font-bold text-white/30">
                                <span className="flex items-center gap-1.5"><Users size={14} /> {game.playerCount}/2 Operators</span>
                                <span className="flex items-center gap-1.5"><Lock size={14} /> Public Sector</span>
                              </div>
                            </div>

                            <Button 
                              onClick={() => joinGame(game.gameId)}
                              disabled={game.isStarted}
                              variant={game.isStarted ? "outline" : "neon"}
                              className="w-full sm:w-auto h-12 px-10 group"
                            >
                              Join Zone <ArrowRight size={16} className="ml-2 group-hover:translate-x-1 transition-transform" />
                            </Button>
                          </div>
                        </CardContent>
                      </Card>
                    </motion.div>
                  ))
                )}
              </AnimatePresence>
            </div>
          </div>
        </div>
      </div>

      {/* Connection Modal */}
      <AnimatePresence>
        {isInGame && (
          <motion.div 
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 z-50 bg-background/80 backdrop-blur-2xl flex items-center justify-center p-6"
          >
            <motion.div 
              initial={{ scale: 0.9, y: 20 }}
              animate={{ scale: 1, y: 0 }}
              className="max-w-xl w-full glass-panel p-12 rounded-[2.5rem] border-primary/30 text-center neon-glow-primary"
            >
              <div className="w-24 h-24 bg-primary/10 rounded-full flex items-center justify-center mx-auto mb-8 border border-primary/20">
                <Activity size={40} className="text-primary animate-pulse" />
              </div>
              <h2 className="text-4xl font-black tracking-tighter uppercase mb-2">Neural Link Established</h2>
              <p className="text-white/40 uppercase tracking-[0.3em] mb-12">Sector: {currentGameId}</p>
              
              <div className="flex items-center justify-center gap-8 mb-12">
                <div className="flex flex-col items-center gap-3">
                  <div className="w-12 h-12 rounded-xl bg-primary/20 flex items-center justify-center">
                    <Users className="text-primary" size={24} />
                  </div>
                  <span className="text-[10px] font-bold text-primary animate-pulse">Syncing Players...</span>
                </div>
                <div className="h-px w-20 bg-gradient-to-r from-transparent via-white/10 to-transparent" />
                <div className="flex flex-col items-center gap-3 opacity-30">
                  <div className="w-12 h-12 rounded-xl bg-white/10 flex items-center justify-center">
                    <Play className="text-white" size={24} />
                  </div>
                  <span className="text-[10px] font-bold">Waiting for Host</span>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <Button 
                  onClick={() => setIsInGame(false)}
                  variant="outline"
                  className="h-14"
                >
                  Abort Sync
                </Button>
                <Button 
                  disabled
                  variant="secondary"
                  className="h-14 opacity-50"
                >
                  Launch Battle
                </Button>
              </div>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Footer Decals */}
      <footer className="fixed bottom-0 left-0 w-full p-8 flex justify-between items-end pointer-events-none opacity-20">
        <div className="mono text-[8px] space-y-1">
          <div>// SECURE_LINE_01_ACTIVE</div>
          <div>// ENCRYPTION_RSA_4096</div>
          <div>// LOC_DATA: {window.location.hostname}</div>
        </div>
        <div className="flex gap-4 items-center">
          <div className="h-4 w-4 border border-white/50 rounded-sm" />
          <div className="h-4 w-4 bg-white/50 rounded-sm" />
          <div className="h-4 w-4 border border-white/50 rounded-sm" />
        </div>
      </footer>
    </div>
  );
}
