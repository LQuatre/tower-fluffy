import React, { useState, useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { MessagePackHubProtocol } from '@microsoft/signalr-protocol-msgpack';
import { motion, AnimatePresence } from 'framer-motion';
import { Server, Users, Play, RefreshCw, Plus, Wifi, WifiOff, Terminal, Shield, Sword } from 'lucide-react';

// Types from server
interface GameInfoDto {
  gameId: string;
  playerCount: number;
  isStarted: boolean;
}

const SERVER_URL = "http://localhost:5128/gameHub";

export default function App() {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [games, setGames] = useState<GameInfoDto[]>([]);
  const [status, setStatus] = useState<'disconnected' | 'connecting' | 'connected' | 'error'>('disconnected');
  const [error, setError] = useState<string | null>(null);
  const [newGameId, setNewGameId] = useState('');
  const [isInGame, setIsInGame] = useState(false);
  const [currentGameId, setCurrentGameId] = useState<string | null>(null);

  const connect = async () => {
    setStatus('connecting');
    setError(null);

    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(SERVER_URL)
      .withHubProtocol(new MessagePackHubProtocol())
      .withAutomaticReconnect()
      .build();

    newConnection.on("ReceiveGameList", (gameList: GameInfoDto[]) => {
      console.log("Received games:", gameList);
      setGames(gameList);
    });

    newConnection.on("ReceiveChat", (sender: string, message: string) => {
      console.log(`[CHAT] ${sender}: ${message}`);
    });

    try {
      await newConnection.start();
      setConnection(newConnection);
      setStatus('connected');
      
      // Get initial games
      await newConnection.invoke("GetActiveGames");
    } catch (err: any) {
      console.error(err);
      setError(err.message);
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
    } catch (err: any) {
      setError(err.message);
    }
  };

  const joinGame = async (gameId: string) => {
    if (!connection) return;
    try {
      await connection.invoke("JoinGame", gameId);
      setCurrentGameId(gameId);
      setIsInGame(true);
      refreshGames();
    } catch (err: any) {
      setError(err.message);
    }
  };

  return (
    <div className="min-h-screen p-8 flex flex-col items-center">
      {/* Header */}
      <motion.header 
        initial={{ y: -50, opacity: 0 }}
        animate={{ y: 0, opacity: 1 }}
        className="w-full max-w-4xl flex justify-between items-center mb-12"
      >
        <div className="flex items-center gap-4">
          <div className="w-12 h-12 glass flex items-center justify-center neon-border-cyan">
            <Shield className="neon-text-cyan" size={24} />
          </div>
          <div>
            <h1 className="text-3xl neon-text-cyan uppercase">TowerFluffy</h1>
            <p className="text-sm text-secondary uppercase tracking-widest">Master Server Protocol v1.0</p>
          </div>
        </div>

        <div className="flex items-center gap-4">
          <div className={`flex items-center gap-2 px-4 py-2 glass rounded-full ${status === 'connected' ? 'neon-border-cyan' : 'border-red-500'}`}>
            {status === 'connected' ? <Wifi size={16} className="text-cyan" /> : <WifiOff size={16} className="text-red-500" />}
            <span className={`text-xs font-bold uppercase ${status === 'connected' ? 'text-cyan' : 'text-red-500'}`}>
              {status}
            </span>
          </div>
          {status !== 'connected' && (
            <button onClick={connect} className="btn-primary">Connect</button>
          )}
        </div>
      </motion.header>

      <main className="w-full max-w-4xl grid grid-cols-1 md:grid-cols-3 gap-8">
        {/* Sidebar / Controls */}
        <section className="md:col-span-1 flex flex-col gap-6">
          <motion.div 
            initial={{ x: -50, opacity: 0 }}
            animate={{ x: 0, opacity: 1 }}
            className="glass p-6 neon-border-pink"
          >
            <h3 className="text-pink mb-4 flex items-center gap-2 uppercase tracking-tighter">
              <Plus size={18} /> New Operation
            </h3>
            <div className="flex flex-col gap-4">
              <input 
                type="text" 
                placeholder="MISSION_ID" 
                value={newGameId}
                onChange={(e) => setNewGameId(e.target.value)}
                className="w-full"
              />
              <button 
                onClick={createGame}
                disabled={!connection || !newGameId}
                className="btn-secondary w-full"
              >
                Deploy
              </button>
            </div>
          </motion.div>

          <motion.div 
            initial={{ x: -50, opacity: 0 }}
            animate={{ x: 0, opacity: 1 }}
            transition={{ delay: 0.1 }}
            className="glass p-6 border-white/5"
          >
            <h3 className="text-white/50 mb-4 flex items-center gap-2 uppercase text-xs">
              <Terminal size={14} /> System Console
            </h3>
            <div className="mono text-[10px] text-secondary h-32 overflow-y-auto flex flex-col gap-1">
              <div>{`> Initializing connection...`}</div>
              {status === 'connected' && <div className="text-cyan">{`> Connected to ${SERVER_URL}`}</div>}
              {error && <div className="text-red-500">{`> ERROR: ${error}`}</div>}
              {games.length > 0 && <div className="text-yellow">{`> Found ${games.length} active sectors`}</div>}
            </div>
          </motion.div>
        </section>

        {/* Game List */}
        <section className="md:col-span-2">
          <div className="flex justify-between items-center mb-6">
            <h2 className="text-2xl uppercase flex items-center gap-3">
              <Server className="text-cyan" /> Active Lobbies
            </h2>
            <button onClick={refreshGames} className="p-2 glass hover:neon-border-cyan text-secondary hover:text-cyan transition-all">
              <RefreshCw size={20} />
            </button>
          </div>

          <div className="grid grid-cols-1 gap-4">
            <AnimatePresence mode="popLayout">
              {games.length === 0 ? (
                <motion.div 
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  className="glass p-12 flex flex-col items-center justify-center text-secondary border-dashed"
                >
                  <WifiOff size={48} className="mb-4 opacity-20" />
                  <p className="uppercase tracking-widest text-sm">No signals detected</p>
                </motion.div>
              ) : (
                games.map((game) => (
                  <motion.div
                    key={game.gameId}
                    initial={{ scale: 0.9, opacity: 0 }}
                    animate={{ scale: 1, opacity: 1 }}
                    exit={{ scale: 0.9, opacity: 0 }}
                    className="glass p-5 flex justify-between items-center group hover:neon-border-cyan transition-all"
                  >
                    <div className="flex items-center gap-6">
                      <div className="w-10 h-10 rounded-lg bg-cyan/10 flex items-center justify-center">
                        <Sword className="text-cyan" size={20} />
                      </div>
                      <div>
                        <h4 className="font-bold text-lg mono tracking-tighter uppercase">{game.gameId}</h4>
                        <div className="flex items-center gap-3 mt-1">
                          <span className="flex items-center gap-1 text-xs text-secondary">
                            <Users size={12} /> {game.playerCount} Players
                          </span>
                          <span className={`text-[10px] px-2 py-0.5 rounded uppercase font-bold ${game.isStarted ? 'bg-red-500/20 text-red-500' : 'bg-green-500/20 text-green-500'}`}>
                            {game.isStarted ? 'Combat In Progress' : 'In Preparation'}
                          </span>
                        </div>
                      </div>
                    </div>
                    
                    <button 
                      onClick={() => joinGame(game.gameId)}
                      disabled={game.isStarted}
                      className={`flex items-center gap-2 px-6 py-2 rounded font-bold uppercase transition-all ${game.isStarted ? 'bg-white/5 text-white/20 cursor-not-allowed' : 'btn-primary'}`}
                    >
                      <Play size={16} fill="currentColor" /> {game.isStarted ? 'Full' : 'Join'}
                    </button>
                  </motion.div>
                ))
              )}
            </AnimatePresence>
          </div>
        </section>
      </main>

      {/* In Game Overlay (Mockup for now) */}
      <AnimatePresence>
        {isInGame && (
          <motion.div 
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 z-50 bg-bg-color/90 backdrop-blur-xl flex flex-col items-center justify-center p-8"
          >
            <motion.div 
              initial={{ scale: 0.8 }}
              animate={{ scale: 1 }}
              className="max-w-2xl w-full glass p-12 neon-border-cyan text-center"
            >
              <h2 className="text-4xl neon-text-cyan mb-2 uppercase">System Linked</h2>
              <p className="text-secondary mb-8 uppercase tracking-widest">Sector: {currentGameId}</p>
              
              <div className="flex justify-center gap-12 mb-12">
                <div className="flex flex-col items-center">
                  <div className="w-24 h-24 rounded-full border-2 border-cyan/30 flex items-center justify-center mb-4">
                    <Users size={40} className="text-cyan" />
                  </div>
                  <span className="text-xs text-secondary uppercase font-bold">Waiting for host</span>
                </div>
                <div className="w-24 h-px bg-cyan/20 self-center" />
                <div className="flex flex-col items-center opacity-50">
                  <div className="w-24 h-24 rounded-full border-2 border-white/10 flex items-center justify-center mb-4">
                    <Play size={40} className="text-white/20" />
                  </div>
                  <span className="text-xs text-white/20 uppercase font-bold">Battle Start</span>
                </div>
              </div>

              <button 
                onClick={() => setIsInGame(false)}
                className="btn-secondary"
              >
                Abort Connection
              </button>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>

      <footer className="mt-auto pt-12 text-secondary/30 text-[10px] uppercase tracking-[0.3em]">
        SignalR Connection Established // Protocol Encrypted // TowerFluffy 2026
      </footer>
    </div>
  );
}
