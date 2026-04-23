import React, { useEffect, useRef } from "react";
import mermaid from "mermaid";
import { 
  ChevronDown
} from "lucide-react";

mermaid.initialize({
  startOnLoad: true,
  theme: "dark",
  securityLevel: "loose",
  fontFamily: "monospace",
  themeVariables: {
    primaryColor: "#3b82f6",
    lineColor: "#64748b",
    fontSize: "16px"
  }
});

const Mermaid = ({ chart }: { chart: string }) => {
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    if (ref.current) {
      mermaid.contentLoaded();
    }
  }, [chart]);

  return (
    <div className="mermaid flex justify-center p-4 bg-slate-900/20 rounded-lg border border-slate-800" ref={ref}>
      {chart}
    </div>
  );
};

const Slide = ({ title, children, step }: { title: string; children: React.ReactNode; step: string }) => (
  <section className="h-screen w-full flex flex-col snap-start bg-[#0a0a0a] text-slate-300 p-8 md:p-20 border-b border-slate-900">
    <div className="max-w-4xl mx-auto w-full flex-1 flex flex-col justify-center">
      <div className="flex items-center gap-4 mb-8">
        <span className="text-sm font-mono text-blue-500">{step}</span>
        <div className="h-[1px] w-12 bg-slate-800" />
        <h2 className="text-3xl font-bold text-white uppercase tracking-tight">{title}</h2>
      </div>

      <div className="space-y-8">
        {children}
      </div>
    </div>
  </section>
);

export default function PresentationPage() {
  return (
    <div className="h-screen overflow-y-auto snap-y snap-mandatory bg-black scroll-smooth">
      
      <section className="h-screen w-full flex flex-col items-center justify-center snap-start bg-black border-b border-slate-900">
        <div className="space-y-4 text-center">
          <h1 className="text-6xl md:text-8xl font-bold text-white uppercase tracking-tighter">
            Tower<span className="text-blue-500">Fluffy</span>
          </h1>
          <div className="h-[1px] w-32 bg-blue-500 mx-auto" />
          <p className="text-xl text-slate-500 uppercase tracking-[0.4em] font-medium">Présentation Technique</p>
        </div>
        <div className="absolute bottom-10 animate-bounce opacity-20">
          <ChevronDown className="text-white h-8 w-8" />
        </div>
      </section>

      <Slide 
        step="01"
        title="Architecture Globale" 
      >
        <p className="text-lg text-slate-400">Séparation en couches pour isoler la logique métier (Domain) des entrées/sorties (UI et Réseau).</p>
        <Mermaid chart={`
graph TD
    UI[UI.Desktop - Avalonia] --> App[Application - Logic de Session]
    App --> Domain[Domain - Simulation & Entités]
    Infra[Infrastructure - SignalR Client] --> App
    Server[Server - ASP.NET Core Hub] -- SignalR --> Infra
        `} />
      </Slide>

      <Slide 
        step="02"
        title="Communication Temps Réel" 
      >
        <p className="text-lg text-slate-400">Protocole de synchronisation basé sur SignalR avec isolation par salles de jeu.</p>
        <Mermaid chart={`
sequenceDiagram
    participant P1 as Pilote A
    participant S as Serveur
    participant P2 as Pilote B
    
    P1->>S: JoinGame("Room-123")
    P2->>S: JoinGame("Room-123")
    P1->>S: SetReady(true)
    S-->>P2: ReceiveOpponentReady(true)
    P2->>S: SetReady(true)
    S-->>P1: ReceiveGameStarted(seed, time)
    S-->>P2: ReceiveGameStarted(seed, time)
        `} />
      </Slide>

      <Slide 
        step="03"
        title="Boucle de Simulation" 
      >
        <p className="text-lg text-slate-400">Le jeu tourne à 60 Ticks par seconde pour garantir un état identique sur tous les clients.</p>
        <Mermaid chart={`
graph LR
    Start(Tick) --> Input[Actions]
    Input --> Move[Déplacement]
    Move --> Combat[Dégâts]
    Combat --> UI[Mise à jour UI]
    UI --> Wait[16ms]
    Wait --> Start
        `} />
      </Slide>

      <Slide 
        step="04"
        title="Points Forts"
      >
        <ul className="space-y-6">
          <li className="flex gap-4">
            <span className="text-blue-500 font-bold">•</span>
            <div>
              <h4 className="text-white font-bold uppercase">Asymétrie Tactique</h4>
              <p className="text-slate-500">Gameplay différencié entre Attaquant et Défenseur.</p>
            </div>
          </li>
          <li className="flex gap-4">
            <span className="text-blue-500 font-bold">•</span>
            <div>
              <h4 className="text-white font-bold uppercase">Optimisation MessagePack</h4>
              <p className="text-slate-500">Échanges binaires ultra-légers pour le réseau.</p>
            </div>
          </li>
          <li className="flex gap-4">
            <span className="text-blue-500 font-bold">•</span>
            <div>
              <h4 className="text-white font-bold uppercase">Multi-plateforme</h4>
              <p className="text-slate-500">Compatible Windows, Linux et macOS nativement.</p>
            </div>
          </li>
        </ul>
      </Slide>

      <Slide 
        step="05"
        title="Scénario de Démo"
      >
        <div className="space-y-4">
          {[
            "1. Connexion au Master Server",
            "2. Création de la salle EXAMEN-2026",
            "3. Sélection des rôles et READY",
            "4. Démonstration du gameplay temps réel"
          ].map((text, i) => (
            <div key={i} className="p-4 border border-slate-800 rounded-lg bg-slate-900/20 text-slate-200">
              {text}
            </div>
          ))}
        </div>
      </Slide>

    </div>
  );
}
