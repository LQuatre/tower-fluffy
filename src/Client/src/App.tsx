import { useState } from 'react'
import * as signalR from '@microsoft/signalr'
import { MessagePackHubProtocol } from '@microsoft/signalr-protocol-msgpack'
import { 
  Server, 
  Users, 
  Play, 
  RefreshCw, 
  Plus, 
  Shield, 
  ArrowRight,
  Globe
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

const getHubUrl = () => {
  const hostname = window.location.hostname;
  const protocol = window.location.protocol === 'https:' ? 'https:' : 'http:';
  return `${protocol}//${hostname}:5128/gameHub`;
};

export default function App() {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [games, setGames] = useState<GameInfoDto[]>([]);
  const [status, setStatus] = useState<'disconnected' | 'connecting' | 'connected' | 'error'>('disconnected');
  const [newGameId, setNewGameId] = useState('');
  const [isInGame, setIsInGame] = useState(false);
  const [currentGameId, setCurrentGameId] = useState<string | null>(null);

  const connect = async () => {
    const hubUrl = getHubUrl();
    setStatus('connecting');

    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withHubProtocol(new MessagePackHubProtocol())
      .withAutomaticReconnect()
      .build();

    newConnection.on("ReceiveGameList", (gameList: GameInfoDto[]) => {
      setGames(gameList);
    });

    try {
      await newConnection.start();
      setConnection(newConnection);
      setStatus('connected');
      await newConnection.invoke("GetActiveGames");
    } catch (err: any) {
      console.error(err);
      setStatus('error');
    }
  };

  const refreshGames = async () => {
    if (connection) {
      await connection.invoke("GetActiveGames");
    }
  };

  const createGame = async () => {
    if (!connection || !newGameId) return;
    try {
      await connection.invoke("JoinGame", newGameId);
      setCurrentGameId(newGameId);
      setIsInGame(true);
      refreshGames();
    } catch (err: any) {}
  };

  const joinGame = async (gameId: string) => {
    if (!connection) return;
    try {
      await connection.invoke("JoinGame", gameId);
      setCurrentGameId(gameId);
      setIsInGame(true);
      refreshGames();
    } catch (err: any) {}
  };

  return (
    <div className="min-h-screen flex flex-col">
      {/* Header */}
      <header className="border-b bg-muted/30 px-6 py-4 flex justify-between items-center">
        <div className="flex items-center gap-3">
          <Shield className="text-primary" size={24} />
          <h1 className="text-lg font-bold tracking-tight">TowerFluffy <span className="text-secondary font-normal ml-2 text-xs uppercase tracking-widest">Master Server</span></h1>
        </div>
        
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2 text-xs uppercase tracking-wider font-bold">
            <div className={cn("w-2 h-2 rounded-full", status === 'connected' ? "bg-green-500" : "bg-red-500")} />
            <span className="text-foreground/60">{status}</span>
          </div>
          {status !== 'connected' && (
            <Button onClick={connect} size="sm" variant={status === 'error' ? 'destructive' : 'default'}>
              {status === 'error' ? 'Retry Connection' : 'Connect'}
            </Button>
          )}
        </div>
      </header>

      <main className="flex-1 max-w-6xl w-full mx-auto p-6 grid grid-cols-1 md:grid-cols-12 gap-8">
        {/* Left column: Create Game */}
        <div className="md:col-span-4 space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="text-sm">Create New Lobby</CardTitle>
              <CardDescription>Enter a name to host a new game session.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <Input 
                placeholder="Game Name / ID" 
                value={newGameId}
                onChange={(e) => setNewGameId(e.target.value)}
              />
              <Button 
                onClick={createGame} 
                disabled={!connection || !newGameId}
                className="w-full"
              >
                <Plus size={16} className="mr-2" /> Host Game
              </Button>
            </CardContent>
          </Card>

          <div className="bg-muted/10 border rounded-lg p-4 text-[11px] text-foreground/40 mono space-y-2">
            <div className="flex justify-between"><span>SERVER_HOST</span> <span>{window.location.hostname}</span></div>
            <div className="flex justify-between"><span>PROTOCOL</span> <span>SignalR/MsgPack</span></div>
            <div className="flex justify-between"><span>UPTIME</span> <span>{Math.floor(performance.now() / 1000)}s</span></div>
          </div>
        </div>

        {/* Right column: Game List */}
        <div className="md:col-span-8">
          <div className="flex justify-between items-center mb-4">
            <h2 className="text-sm font-bold uppercase tracking-widest text-foreground/60 flex items-center gap-2">
              <Globe size={14} /> Available Sessions
            </h2>
            <Button onClick={refreshGames} variant="ghost" size="icon" className="h-8 w-8">
              <RefreshCw size={14} />
            </Button>
          </div>

          <div className="space-y-3">
            {games.length === 0 ? (
              <div className="border border-dashed rounded-lg p-12 text-center text-foreground/30">
                <Server size={32} className="mx-auto mb-4 opacity-20" />
                <p className="text-sm">No active games found. Connect and create one to start.</p>
              </div>
            ) : (
              games.map((game) => (
                <div key={game.gameId} className="flex items-center justify-between p-4 bg-muted/20 border rounded-lg hover:border-primary/50 transition-colors">
                  <div className="flex items-center gap-4">
                    <div className="w-10 h-10 bg-primary/10 rounded flex items-center justify-center">
                      <Play size={18} className="text-primary fill-primary/20" />
                    </div>
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="font-bold">{game.gameId}</span>
                        <Badge variant={game.isStarted ? "destructive" : "secondary"} className="text-[9px] h-4 px-1.5">
                          {game.isStarted ? "In Progress" : "Lobby"}
                        </Badge>
                      </div>
                      <div className="text-[11px] text-foreground/40 flex items-center gap-2 mt-0.5">
                        <Users size={12} /> {game.playerCount} / 2 Players
                      </div>
                    </div>
                  </div>
                  
                  <Button 
                    onClick={() => joinGame(game.gameId)}
                    disabled={game.isStarted}
                    size="sm"
                    variant="outline"
                  >
                    Join <ArrowRight size={14} className="ml-2" />
                  </Button>
                </div>
              ))
            )}
          </div>
        </div>
      </main>

      {/* Simplified In-Game State */}
      {isInGame && (
        <div className="fixed inset-0 bg-background/90 backdrop-blur-sm flex items-center justify-center p-6 z-50">
          <div className="max-w-md w-full p-8 border bg-card rounded-lg text-center shadow-xl">
            <h2 className="text-2xl font-bold mb-2 uppercase tracking-tight">Connected</h2>
            <p className="text-sm text-foreground/60 mb-8 font-mono">{currentGameId}</p>
            
            <div className="p-4 bg-primary/5 border border-primary/20 rounded-md mb-8 text-sm text-primary">
              <Users size={16} className="inline mr-2 animate-pulse" />
              Waiting for match to start...
            </div>

            <Button onClick={() => setIsInGame(false)} variant="outline" className="w-full">
              Leave Lobby
            </Button>
          </div>
        </div>
      )}

      <footer className="p-4 text-center border-t bg-muted/10 text-[10px] text-foreground/20 uppercase tracking-[0.2em]">
        TowerFluffy Game Server Management Console
      </footer>
    </div>
  );
}
